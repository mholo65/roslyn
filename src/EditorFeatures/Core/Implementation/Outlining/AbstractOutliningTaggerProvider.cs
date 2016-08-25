// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.Text.Tagging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    /// <summary>
    /// Shared implementation of the outliner tagger provider.
    /// 
    /// Note: the outliner tagger is a normal buffer tagger provider and not a view tagger provider.
    /// This is important for two reasons.  The first is that if it were view-based then we would lose
    /// the state of the collapsed/open regions when they scrolled in and out of view.  Also, if the
    /// editor doesn't know about all the regions in the file, then it wouldn't be able to
    /// persist them to the SUO file to persist this data across sessions.
    /// </summary>
    internal abstract partial class AbstractOutliningTaggerProvider<TRegionTag> : 
        AsynchronousTaggerProvider<TRegionTag>,
        IEqualityComparer<TRegionTag>
        where TRegionTag : ITag
    {
        public const string OutliningRegionTextViewRole = nameof(OutliningRegionTextViewRole);

        protected readonly ITextEditorFactoryService TextEditorFactoryService;
        protected readonly IEditorOptionsFactoryService EditorOptionsFactoryService;
        protected readonly IProjectionBufferFactoryService ProjectionBufferFactoryService;

        protected AbstractOutliningTaggerProvider(
            IForegroundNotificationService notificationService,
            ITextEditorFactoryService textEditorFactoryService,
            IEditorOptionsFactoryService editorOptionsFactoryService,
            IProjectionBufferFactoryService projectionBufferFactoryService,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
                : base(new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.Outlining), notificationService)
        {
            TextEditorFactoryService = textEditorFactoryService;
            EditorOptionsFactoryService = editorOptionsFactoryService;
            ProjectionBufferFactoryService = projectionBufferFactoryService;
        }

        protected sealed override IEqualityComparer<TRegionTag> TagComparer => this;

        public abstract bool Equals(TRegionTag x, TRegionTag y);

        public abstract int GetHashCode(TRegionTag obj);

        protected sealed override ITaggerEventSource CreateEventSource(ITextView textViewOpt, ITextBuffer subjectBuffer)
        {
            // We listen to the following events:
            // 1) Text changes.  These can obviously affect outlining, so we need to recompute when
            //     we hear about them.
            // 2) Parse option changes.  These can affect outlining when, for example, we change from 
            //    DEBUG to RELEASE (affecting the inactive/active regions).
            // 3) When we hear about a workspace being registered.  Outlining may run before a 
            //    we even know about a workspace.  This can happen, for example, in the TypeScript
            //    case.  With TypeScript a file is opened, but the workspace is not generated until
            //    some time later when they have examined the file system.  As such, initially,
            //    the file will not have outline spans.  When the workspace is created, we want to
            //    then produce the right outlining spans.
            return TaggerEventSources.Compose(
                TaggerEventSources.OnTextChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnParseOptionChanged(subjectBuffer, TaggerDelay.OnIdle),
                TaggerEventSources.OnWorkspaceRegistrationChanged(subjectBuffer, TaggerDelay.OnIdle));
        }

        /// <summary>
        /// Keep this in sync with <see cref="ProduceTagsSynchronously"/>
        /// </summary>
        protected sealed override async Task ProduceTagsAsync(
            TaggerContext<TRegionTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                var outliningService = TryGetOutliningService(context, documentSnapshotSpan);
                if (outliningService != null)
                {
                    var regions = await outliningService.GetOutliningSpansAsync(
                        documentSnapshotSpan.Document, context.CancellationToken).ConfigureAwait(false);
                    ProcessOutliningSpans(context, documentSnapshotSpan.SnapshotSpan, outliningService, regions);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Keep this in sync with <see cref="ProduceTagsAsync"/>
        /// </summary>
        protected sealed override void ProduceTagsSynchronously(
            TaggerContext<TRegionTag> context, DocumentSnapshotSpan documentSnapshotSpan, int? caretPosition)
        {
            try
            {
                var outliningService = TryGetOutliningService(context, documentSnapshotSpan);
                if (outliningService != null)
                {
                    var document = documentSnapshotSpan.Document;
                    var cancellationToken = context.CancellationToken;

                    // Try to call through the synchronous service if possible. Otherwise, fallback
                    // and make a blocking call against the async service.
                    var synchronousOutliningService = outliningService as ISynchronousOutliningService;

                    var regions = synchronousOutliningService != null
                        ? synchronousOutliningService.GetOutliningSpans(document, cancellationToken)
                        : outliningService.GetOutliningSpansAsync(document, cancellationToken).WaitAndGetResult(cancellationToken);

                    ProcessOutliningSpans(context, documentSnapshotSpan.SnapshotSpan, outliningService, regions);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private IOutliningService TryGetOutliningService(
            TaggerContext<TRegionTag> context,
            DocumentSnapshotSpan documentSnapshotSpan)
        {
            var cancellationToken = context.CancellationToken;
            using (Logger.LogBlock(FunctionId.Tagger_Outlining_TagProducer_ProduceTags, cancellationToken))
            {
                var document = documentSnapshotSpan.Document;
                var snapshotSpan = documentSnapshotSpan.SnapshotSpan;
                var snapshot = snapshotSpan.Snapshot;

                if (document != null)
                {
                    return document.Project.LanguageServices.GetService<IOutliningService>();
                }
            }

            return null;
        }

        private void ProcessOutliningSpans(
            TaggerContext<TRegionTag> context, SnapshotSpan snapshotSpan, IOutliningService outliningService, IList<OutliningSpan> regions)
        {
            if (regions != null)
            {
                var snapshot = snapshotSpan.Snapshot;
                regions = GetMultiLineRegions(outliningService, regions, snapshot);

                // Create the outlining tags.
                var tagSpanStack = new Stack<TagSpan<TRegionTag>>();

                foreach (var region in regions)
                {
                    var spanToCollapse = new SnapshotSpan(snapshot, region.TextSpan.ToSpan());

                    while (tagSpanStack.Count > 0 && 
                           tagSpanStack.Peek().Span.End <= spanToCollapse.Span.Start)
                    {
                        tagSpanStack.Pop();
                    }

                    var parentTag = tagSpanStack.Count > 0 ? tagSpanStack.Peek() : null;
                    var tag = CreateTag(parentTag.Tag, snapshot, region);

                    var tagSpan = new TagSpan<TRegionTag>(spanToCollapse, tag);

                    context.AddTag(tagSpan);
                    tagSpanStack.Push(tagSpan);
                }
            }
        }

        protected abstract TRegionTag CreateTag(
            TRegionTag parentTag, ITextSnapshot snapshot, OutliningSpan region);

        private static bool s_exceptionReported = false;

        private List<OutliningSpan> GetMultiLineRegions(IOutliningService service, IList<OutliningSpan> regions, ITextSnapshot snapshot)
        {
            // Remove any spans that aren't multiline.
            var multiLineRegions = new List<OutliningSpan>(regions.Count);
            foreach (var region in regions)
            {
                if (region != null && region.TextSpan.Length > 0)
                {
                    // Check if any clients produced an invalid OutliningSpan.  If so, filter them
                    // out and report a non-fatal watson so we can attempt to determine the source
                    // of the issue.
                    var snapshotSpan = snapshot.GetFullSpan().Span;
                    var regionSpan = region.TextSpan.ToSpan();
                    if (!snapshotSpan.Contains(regionSpan))
                    {
                        if (!s_exceptionReported)
                        {
                            s_exceptionReported = true;
                            try
                            {
                                throw new InvalidOutliningRegionException(service, snapshot, snapshotSpan, regionSpan);
                            }
                            catch (InvalidOutliningRegionException e) when (FatalError.ReportWithoutCrash(e))
                            {
                            }
                        }
                        continue;
                    }

                    var startLine = snapshot.GetLineNumberFromPosition(region.TextSpan.Start);
                    var endLine = snapshot.GetLineNumberFromPosition(region.TextSpan.End);
                    if (startLine != endLine)
                    {
                        multiLineRegions.Add(region);
                    }
                }
            }

            // Make sure the regions are lexicographically sorted.  This is needed
            // so we can appropriately parent them for BlockTags.
            multiLineRegions.Sort((s1, s2) => s1.TextSpan.Start - s2.TextSpan.Start);
            return multiLineRegions;
        }
    }
}
