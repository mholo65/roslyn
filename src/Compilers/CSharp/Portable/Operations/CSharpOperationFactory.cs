﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Operations
{
    internal sealed partial class CSharpOperationFactory
    {
        private readonly ConcurrentDictionary<BoundNode, IOperation> _cache =
            new ConcurrentDictionary<BoundNode, IOperation>(concurrencyLevel: 2, capacity: 10);

        private readonly SemanticModel _semanticModel;

        public CSharpOperationFactory(SemanticModel semanticModel)
        {
            _semanticModel = semanticModel;
        }

        public IOperation Create(BoundNode boundNode)
        {
            if (boundNode == null)
            {
                return null;
            }

            // implicit receiver can be shared between multiple bound nodes.
            // always return cloned one
            if (boundNode.Kind == BoundKind.ImplicitReceiver)
            {
                return _semanticModel.CloneOperation(CreateInternal(boundNode));
            }

            return _cache.GetOrAdd(boundNode, n => CreateInternal(n));
        }

        private IOperation CreateInternal(BoundNode boundNode)
        {
            switch (boundNode.Kind)
            {
                case BoundKind.DeconstructValuePlaceholder:
                    return CreateBoundDeconstructValuePlaceholderOperation((BoundDeconstructValuePlaceholder)boundNode);
                case BoundKind.DeconstructionAssignmentOperator:
                    return CreateBoundDeconstructionAssignmentOperator((BoundDeconstructionAssignmentOperator)boundNode);
                case BoundKind.Call:
                    return CreateBoundCallOperation((BoundCall)boundNode);
                case BoundKind.Local:
                    return CreateBoundLocalOperation((BoundLocal)boundNode);
                case BoundKind.FieldAccess:
                    return CreateBoundFieldAccessOperation((BoundFieldAccess)boundNode);
                case BoundKind.PropertyAccess:
                    return CreateBoundPropertyAccessOperation((BoundPropertyAccess)boundNode);
                case BoundKind.IndexerAccess:
                    return CreateBoundIndexerAccessOperation((BoundIndexerAccess)boundNode);
                case BoundKind.EventAccess:
                    return CreateBoundEventAccessOperation((BoundEventAccess)boundNode);
                case BoundKind.EventAssignmentOperator:
                    return CreateBoundEventAssignmentOperatorOperation((BoundEventAssignmentOperator)boundNode);
                case BoundKind.Parameter:
                    return CreateBoundParameterOperation((BoundParameter)boundNode);
                case BoundKind.Literal:
                    return CreateBoundLiteralOperation((BoundLiteral)boundNode);
                case BoundKind.DynamicInvocation:
                    return CreateBoundDynamicInvocationExpressionOperation((BoundDynamicInvocation)boundNode);
                case BoundKind.DynamicIndexerAccess:
                    return CreateBoundDynamicIndexerAccessExpressionOperation((BoundDynamicIndexerAccess)boundNode);
                case BoundKind.ObjectCreationExpression:
                    return CreateBoundObjectCreationExpressionOperation((BoundObjectCreationExpression)boundNode);
                case BoundKind.DynamicObjectCreationExpression:
                    return CreateBoundDynamicObjectCreationExpressionOperation((BoundDynamicObjectCreationExpression)boundNode);
                case BoundKind.ObjectInitializerExpression:
                    return CreateBoundObjectInitializerExpressionOperation((BoundObjectInitializerExpression)boundNode);
                case BoundKind.CollectionInitializerExpression:
                    return CreateBoundCollectionInitializerExpressionOperation((BoundCollectionInitializerExpression)boundNode);
                case BoundKind.ObjectInitializerMember:
                    return CreateBoundObjectInitializerMemberOperation((BoundObjectInitializerMember)boundNode);
                case BoundKind.CollectionElementInitializer:
                    return CreateBoundCollectionElementInitializerOperation((BoundCollectionElementInitializer)boundNode);
                case BoundKind.DynamicMemberAccess:
                    return CreateBoundDynamicMemberAccessOperation((BoundDynamicMemberAccess)boundNode);
                case BoundKind.DynamicCollectionElementInitializer:
                    return CreateBoundDynamicCollectionElementInitializerOperation((BoundDynamicCollectionElementInitializer)boundNode);
                case BoundKind.UnboundLambda:
                    return CreateUnboundLambdaOperation((UnboundLambda)boundNode);
                case BoundKind.Lambda:
                    return CreateBoundLambdaOperation((BoundLambda)boundNode);
                case BoundKind.Conversion:
                    return CreateBoundConversionOperation((BoundConversion)boundNode);
                case BoundKind.AsOperator:
                    return CreateBoundAsOperatorOperation((BoundAsOperator)boundNode);
                case BoundKind.IsOperator:
                    return CreateBoundIsOperatorOperation((BoundIsOperator)boundNode);
                case BoundKind.SizeOfOperator:
                    return CreateBoundSizeOfOperatorOperation((BoundSizeOfOperator)boundNode);
                case BoundKind.TypeOfOperator:
                    return CreateBoundTypeOfOperatorOperation((BoundTypeOfOperator)boundNode);
                case BoundKind.ArrayCreation:
                    return CreateBoundArrayCreationOperation((BoundArrayCreation)boundNode);
                case BoundKind.ArrayInitialization:
                    return CreateBoundArrayInitializationOperation((BoundArrayInitialization)boundNode);
                case BoundKind.DefaultExpression:
                    return CreateBoundDefaultExpressionOperation((BoundDefaultExpression)boundNode);
                case BoundKind.BaseReference:
                    return CreateBoundBaseReferenceOperation((BoundBaseReference)boundNode);
                case BoundKind.ThisReference:
                    return CreateBoundThisReferenceOperation((BoundThisReference)boundNode);
                case BoundKind.AssignmentOperator:
                    return CreateBoundAssignmentOperatorOrMemberInitializerOperation((BoundAssignmentOperator)boundNode);
                case BoundKind.CompoundAssignmentOperator:
                    return CreateBoundCompoundAssignmentOperatorOperation((BoundCompoundAssignmentOperator)boundNode);
                case BoundKind.IncrementOperator:
                    return CreateBoundIncrementOperatorOperation((BoundIncrementOperator)boundNode);
                case BoundKind.BadExpression:
                    return CreateBoundBadExpressionOperation((BoundBadExpression)boundNode);
                case BoundKind.NewT:
                    return CreateBoundNewTOperation((BoundNewT)boundNode);
                case BoundKind.UnaryOperator:
                    return CreateBoundUnaryOperatorOperation((BoundUnaryOperator)boundNode);
                case BoundKind.BinaryOperator:
                    return CreateBoundBinaryOperatorOperation((BoundBinaryOperator)boundNode);
                case BoundKind.ConditionalOperator:
                    return CreateBoundConditionalOperatorOperation((BoundConditionalOperator)boundNode);
                case BoundKind.NullCoalescingOperator:
                    return CreateBoundNullCoalescingOperatorOperation((BoundNullCoalescingOperator)boundNode);
                case BoundKind.AwaitExpression:
                    return CreateBoundAwaitExpressionOperation((BoundAwaitExpression)boundNode);
                case BoundKind.ArrayAccess:
                    return CreateBoundArrayAccessOperation((BoundArrayAccess)boundNode);
                case BoundKind.NameOfOperator:
                    return CreateBoundNameOfOperatorOperation((BoundNameOfOperator)boundNode);
                case BoundKind.ThrowExpression:
                    return CreateBoundThrowExpressionOperation((BoundThrowExpression)boundNode);
                case BoundKind.AddressOfOperator:
                    return CreateBoundAddressOfOperatorOperation((BoundAddressOfOperator)boundNode);
                case BoundKind.ImplicitReceiver:
                    return CreateBoundImplicitReceiverOperation((BoundImplicitReceiver)boundNode);
                case BoundKind.ConditionalAccess:
                    return CreateBoundConditionalAccessOperation((BoundConditionalAccess)boundNode);
                case BoundKind.ConditionalReceiver:
                    return CreateBoundConditionalReceiverOperation((BoundConditionalReceiver)boundNode);
                case BoundKind.FieldEqualsValue:
                    return CreateBoundFieldEqualsValueOperation((BoundFieldEqualsValue)boundNode);
                case BoundKind.PropertyEqualsValue:
                    return CreateBoundPropertyEqualsValueOperation((BoundPropertyEqualsValue)boundNode);
                case BoundKind.ParameterEqualsValue:
                    return CreateBoundParameterEqualsValueOperation((BoundParameterEqualsValue)boundNode);
                case BoundKind.Block:
                    return CreateBoundBlockOperation((BoundBlock)boundNode);
                case BoundKind.ContinueStatement:
                    return CreateBoundContinueStatementOperation((BoundContinueStatement)boundNode);
                case BoundKind.BreakStatement:
                    return CreateBoundBreakStatementOperation((BoundBreakStatement)boundNode);
                case BoundKind.YieldBreakStatement:
                    return CreateBoundYieldBreakStatementOperation((BoundYieldBreakStatement)boundNode);
                case BoundKind.GotoStatement:
                    return CreateBoundGotoStatementOperation((BoundGotoStatement)boundNode);
                case BoundKind.NoOpStatement:
                    return CreateBoundNoOpStatementOperation((BoundNoOpStatement)boundNode);
                case BoundKind.IfStatement:
                    return CreateBoundIfStatementOperation((BoundIfStatement)boundNode);
                case BoundKind.WhileStatement:
                    return CreateBoundWhileStatementOperation((BoundWhileStatement)boundNode);
                case BoundKind.DoStatement:
                    return CreateBoundDoStatementOperation((BoundDoStatement)boundNode);
                case BoundKind.ForStatement:
                    return CreateBoundForStatementOperation((BoundForStatement)boundNode);
                case BoundKind.ForEachStatement:
                    return CreateBoundForEachStatementOperation((BoundForEachStatement)boundNode);
                case BoundKind.SwitchStatement:
                    return CreateBoundSwitchStatementOperation((BoundSwitchStatement)boundNode);
                case BoundKind.SwitchLabel:
                    return CreateBoundSwitchLabelOperation((BoundSwitchLabel)boundNode);
                case BoundKind.TryStatement:
                    return CreateBoundTryStatementOperation((BoundTryStatement)boundNode);
                case BoundKind.CatchBlock:
                    return CreateBoundCatchBlockOperation((BoundCatchBlock)boundNode);
                // https://github.com/dotnet/roslyn/issues/21281
                //case BoundKind.FixedStatement:
                //    return CreateBoundFixedStatementOperation((BoundFixedStatement)boundNode);
                case BoundKind.UsingStatement:
                    return CreateBoundUsingStatementOperation((BoundUsingStatement)boundNode);
                case BoundKind.ThrowStatement:
                    return CreateBoundThrowStatementOperation((BoundThrowStatement)boundNode);
                case BoundKind.ReturnStatement:
                    return CreateBoundReturnStatementOperation((BoundReturnStatement)boundNode);
                case BoundKind.YieldReturnStatement:
                    return CreateBoundYieldReturnStatementOperation((BoundYieldReturnStatement)boundNode);
                case BoundKind.LockStatement:
                    return CreateBoundLockStatementOperation((BoundLockStatement)boundNode);
                case BoundKind.BadStatement:
                    return CreateBoundBadStatementOperation((BoundBadStatement)boundNode);
                case BoundKind.LocalDeclaration:
                    return CreateBoundLocalDeclarationOperation((BoundLocalDeclaration)boundNode);
                case BoundKind.MultipleLocalDeclarations:
                    return CreateBoundMultipleLocalDeclarationsOperation((BoundMultipleLocalDeclarations)boundNode);
                case BoundKind.LabelStatement:
                    return CreateBoundLabelStatementOperation((BoundLabelStatement)boundNode);
                case BoundKind.LabeledStatement:
                    return CreateBoundLabeledStatementOperation((BoundLabeledStatement)boundNode);
                case BoundKind.ExpressionStatement:
                    return CreateBoundExpressionStatementOperation((BoundExpressionStatement)boundNode);
                case BoundKind.TupleLiteral:
                case BoundKind.ConvertedTupleLiteral:
                    return CreateBoundTupleExpressionOperation((BoundTupleExpression)boundNode);
                case BoundKind.InterpolatedString:
                    return CreateBoundInterpolatedStringExpressionOperation((BoundInterpolatedString)boundNode);
                case BoundKind.StringInsert:
                    return CreateBoundInterpolationOperation((BoundStringInsert)boundNode);
                case BoundKind.LocalFunctionStatement:
                    return CreateBoundLocalFunctionStatementOperation((BoundLocalFunctionStatement)boundNode);
                case BoundKind.AnonymousObjectCreationExpression:
                    return CreateBoundAnonymousObjectCreationExpressionOperation((BoundAnonymousObjectCreationExpression)boundNode);
                case BoundKind.AnonymousPropertyDeclaration:
                    return CreateBoundAnonymousPropertyDeclarationOperation((BoundAnonymousPropertyDeclaration)boundNode);
                case BoundKind.ConstantPattern:
                    return CreateBoundConstantPatternOperation((BoundConstantPattern)boundNode);
                case BoundKind.DeclarationPattern:
                    return CreateBoundDeclarationPatternOperation((BoundDeclarationPattern)boundNode);
                case BoundKind.WildcardPattern:
                    throw ExceptionUtilities.Unreachable;
                case BoundKind.PatternSwitchStatement:
                    return CreateBoundPatternSwitchStatementOperation((BoundPatternSwitchStatement)boundNode);
                case BoundKind.PatternSwitchLabel:
                    return CreateBoundPatternSwitchLabelOperation((BoundPatternSwitchLabel)boundNode);
                case BoundKind.IsPatternExpression:
                    return CreateBoundIsPatternExpressionOperation((BoundIsPatternExpression)boundNode);
                case BoundKind.QueryClause:
                    return CreateBoundQueryClauseOperation((BoundQueryClause)boundNode);
                case BoundKind.DelegateCreationExpression:
                    return CreateBoundDelegateCreationExpressionOperation((BoundDelegateCreationExpression)boundNode);
                default:
                    Optional<object> constantValue = ConvertToOptional((boundNode as BoundExpression)?.ConstantValue);
                    bool isImplicit = boundNode.WasCompilerGenerated;

                    return Operation.CreateOperationNone(_semanticModel, boundNode.Syntax, constantValue, getChildren: () => GetIOperationChildren(boundNode), isImplicit: isImplicit);
            }
        }

        private ImmutableArray<IOperation> GetIOperationChildren(BoundNode boundNode)
        {
            var boundNodeWithChildren = (IBoundNodeWithIOperationChildren)boundNode;
            var children = boundNodeWithChildren.Children;
            if (children.IsDefaultOrEmpty)
            {
                return ImmutableArray<IOperation>.Empty;
            }

            var builder = ArrayBuilder<IOperation>.GetInstance(children.Length);
            foreach (BoundNode childNode in children)
            {
                IOperation operation = Create(childNode);
                if (operation == null)
                {
                    continue;
                }

                builder.Add(operation);
            }

            return builder.ToImmutableAndFree();
        }

        private IVariableDeclarationOperation CreateVariableDeclaration(BoundLocalDeclaration boundNode)
        {
            return (IVariableDeclarationOperation)_cache.GetOrAdd(boundNode, n => CreateVariableDeclarationInternal((BoundLocalDeclaration)n, n.Syntax));
        }

        private IPlaceholderOperation CreateBoundDeconstructValuePlaceholderOperation(BoundDeconstructValuePlaceholder boundDeconstructValuePlaceholder)
        {
            SyntaxNode syntax = boundDeconstructValuePlaceholder.Syntax;
            ITypeSymbol type = boundDeconstructValuePlaceholder.Type;
            Optional<object> constantValue = ConvertToOptional(boundDeconstructValuePlaceholder.ConstantValue);
            bool isImplicit = boundDeconstructValuePlaceholder.WasCompilerGenerated;
            return new PlaceholderExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDeconstructionAssignmentOperation CreateBoundDeconstructionAssignmentOperator(BoundDeconstructionAssignmentOperator boundDeconstructionAssignmentOperator)
        {
            Lazy<IOperation> target = new Lazy<IOperation>(() => Create(boundDeconstructionAssignmentOperator.Left));
            // Skip the synthetic deconstruction conversion wrapping the right operand.
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundDeconstructionAssignmentOperator.Right.Operand));
            SyntaxNode syntax = boundDeconstructionAssignmentOperator.Syntax;
            ITypeSymbol type = boundDeconstructionAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundDeconstructionAssignmentOperator.ConstantValue);
            bool isImplicit = boundDeconstructionAssignmentOperator.WasCompilerGenerated;
            return new LazyDeconstructionAssignmentExpression(target, value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundCallOperation(BoundCall boundCall)
        {
            MethodSymbol targetMethod = boundCall.Method;
            SyntaxNode syntax = boundCall.Syntax;
            ITypeSymbol type = boundCall.Type;
            Optional<object> constantValue = ConvertToOptional(boundCall.ConstantValue);
            bool isImplicit = boundCall.WasCompilerGenerated;
            
            if (!boundCall.OriginalMethodsOpt.IsDefault || boundCall.ResultKind == LookupResultKind.OverloadResolutionFailure || targetMethod == null || targetMethod.OriginalDefinition is ErrorMethodSymbol)
            {
                return CreateInvalidExpressionForHasArgumentsExpression(boundCall.ReceiverOpt, boundCall.Arguments, null, syntax, type, constantValue, isImplicit);
            }

            Lazy<IOperation> instance = new Lazy<IOperation>(() => CreateBoundCallInstanceOperation(boundCall));
            bool isVirtual = (object)targetMethod != null &&
                        boundCall.ReceiverOpt != null &&
                        (targetMethod.IsVirtual || targetMethod.IsAbstract || targetMethod.IsOverride) &&
                        !boundCall.ReceiverOpt.SuppressVirtualCalls;
            Lazy<ImmutableArray<IArgumentOperation>> arguments = new Lazy<ImmutableArray<IArgumentOperation>>(() =>
            {
                return DeriveArguments(
                    boundCall,
                    boundCall.BinderOpt,
                    targetMethod,
                    targetMethod,
                    boundCall.Arguments,
                    boundCall.ArgumentNamesOpt,
                    boundCall.ArgsToParamsOpt,
                    boundCall.ArgumentRefKindsOpt,
                    boundCall.Method.Parameters,
                    boundCall.Expanded,
                    syntax,
                    boundCall.InvokedAsExtensionMethod);
            });
            return new LazyInvocationExpression(targetMethod, instance, isVirtual, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundLocalOperation(BoundLocal boundLocal)
        {
            ILocalSymbol local = boundLocal.LocalSymbol;
            bool isDeclaration = boundLocal.IsDeclaration;
            SyntaxNode syntax = boundLocal.Syntax;
            ITypeSymbol type = boundLocal.Type;
            Optional<object> constantValue = ConvertToOptional(boundLocal.ConstantValue);
            bool isImplicit = boundLocal.WasCompilerGenerated;
            if (isDeclaration && syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;
                Lazy<IOperation> localReferenceExpression = new Lazy<IOperation>(() =>
                    new LocalReferenceExpression(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit));
                return new LazyDeclarationExpression(localReferenceExpression, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
            }
            return new LocalReferenceExpression(local, isDeclaration, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundFieldAccessOperation(BoundFieldAccess boundFieldAccess)
        {
            IFieldSymbol field = boundFieldAccess.FieldSymbol;
            bool isDeclaration = boundFieldAccess.IsDeclaration;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => Create(boundFieldAccess.FieldSymbol.IsStatic ? null : boundFieldAccess.ReceiverOpt));
            SyntaxNode syntax = boundFieldAccess.Syntax;
            ITypeSymbol type = boundFieldAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundFieldAccess.ConstantValue);
            bool isImplicit = boundFieldAccess.WasCompilerGenerated;
            if (isDeclaration && syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                syntax = declarationExpressionSyntax.Designation;
                Lazy<IOperation> fieldReferenceExpression = new Lazy<IOperation>(() =>
                    new LazyFieldReferenceExpression(field, isDeclaration, instance, _semanticModel, syntax, type, constantValue, isImplicit));
                return new LazyDeclarationExpression(fieldReferenceExpression, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
            }
            return new LazyFieldReferenceExpression(field, isDeclaration, instance, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IPropertyReferenceOperation CreateBoundPropertyAccessOperation(BoundPropertyAccess boundPropertyAccess)
        {
            IPropertySymbol property = boundPropertyAccess.PropertySymbol;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => Create(boundPropertyAccess.PropertySymbol.IsStatic ? null : boundPropertyAccess.ReceiverOpt));
            Lazy<ImmutableArray<IArgumentOperation>> arguments = new Lazy<ImmutableArray<IArgumentOperation>>(() => ImmutableArray<IArgumentOperation>.Empty);
            SyntaxNode syntax = boundPropertyAccess.Syntax;
            ITypeSymbol type = boundPropertyAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundPropertyAccess.ConstantValue);
            bool isImplicit = boundPropertyAccess.WasCompilerGenerated;
            return new LazyPropertyReferenceExpression(property, instance, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundIndexerAccessOperation(BoundIndexerAccess boundIndexerAccess)
        {
            PropertySymbol property = boundIndexerAccess.Indexer;
            SyntaxNode syntax = boundIndexerAccess.Syntax;
            ITypeSymbol type = boundIndexerAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundIndexerAccess.ConstantValue);
            bool isImplicit = boundIndexerAccess.WasCompilerGenerated;

            MethodSymbol accessor = boundIndexerAccess.UseSetterForDefaultArgumentGeneration
                ? property.GetOwnOrInheritedSetMethod()
                : property.GetOwnOrInheritedGetMethod();
            
            if (!boundIndexerAccess.OriginalIndexersOpt.IsDefault || boundIndexerAccess.ResultKind == LookupResultKind.OverloadResolutionFailure || accessor == null || accessor.OriginalDefinition is ErrorMethodSymbol)
            {
                return CreateInvalidExpressionForHasArgumentsExpression(boundIndexerAccess.ReceiverOpt, boundIndexerAccess.Arguments, null, syntax, type, constantValue, isImplicit);
            }

            Lazy<IOperation> instance = new Lazy<IOperation>(() => Create(property.IsStatic ? null : boundIndexerAccess.ReceiverOpt));
            Lazy<ImmutableArray<IArgumentOperation>> arguments = new Lazy<ImmutableArray<IArgumentOperation>>(() =>
                DeriveArguments(
                    boundIndexerAccess,
                    boundIndexerAccess.BinderOpt,
                    property,
                    accessor,
                    boundIndexerAccess.Arguments,
                    boundIndexerAccess.ArgumentNamesOpt,
                    boundIndexerAccess.ArgsToParamsOpt,
                    boundIndexerAccess.ArgumentRefKindsOpt,
                    property.Parameters,
                    boundIndexerAccess.Expanded,
                    syntax));
            return new LazyPropertyReferenceExpression(property, instance, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEventReferenceOperation CreateBoundEventAccessOperation(BoundEventAccess boundEventAccess)
        {
            IEventSymbol @event = boundEventAccess.EventSymbol;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => Create(boundEventAccess.EventSymbol.IsStatic ? null : boundEventAccess.ReceiverOpt));
            SyntaxNode syntax = boundEventAccess.Syntax;
            ITypeSymbol type = boundEventAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundEventAccess.ConstantValue);
            bool isImplicit = boundEventAccess.WasCompilerGenerated;
            return new LazyEventReferenceExpression(@event, instance, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEventAssignmentOperation CreateBoundEventAssignmentOperatorOperation(BoundEventAssignmentOperator boundEventAssignmentOperator)
        {
            Lazy<IEventReferenceOperation> eventReference = new Lazy<IEventReferenceOperation>(() => CreateBoundEventAccessOperation(boundEventAssignmentOperator));
            Lazy<IOperation> handlerValue = new Lazy<IOperation>(() => Create(boundEventAssignmentOperator.Argument));
            SyntaxNode syntax = boundEventAssignmentOperator.Syntax;
            bool adds = boundEventAssignmentOperator.IsAddition;
            ITypeSymbol type = boundEventAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundEventAssignmentOperator.ConstantValue);
            bool isImplicit = boundEventAssignmentOperator.WasCompilerGenerated;
            return new LazyEventAssignmentOperation(eventReference, handlerValue, adds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IParameterReferenceOperation CreateBoundParameterOperation(BoundParameter boundParameter)
        {
            IParameterSymbol parameter = boundParameter.ParameterSymbol;
            SyntaxNode syntax = boundParameter.Syntax;
            ITypeSymbol type = boundParameter.Type;
            Optional<object> constantValue = ConvertToOptional(boundParameter.ConstantValue);
            bool isImplicit = boundParameter.WasCompilerGenerated;
            return new ParameterReferenceExpression(parameter, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILiteralOperation CreateBoundLiteralOperation(BoundLiteral boundLiteral)
        {
            SyntaxNode syntax = boundLiteral.Syntax;
            ITypeSymbol type = boundLiteral.Type;
            Optional<object> constantValue = ConvertToOptional(boundLiteral.ConstantValue);
            bool isImplicit = boundLiteral.WasCompilerGenerated;
            return new LiteralExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAnonymousObjectCreationOperation CreateBoundAnonymousObjectCreationExpressionOperation(BoundAnonymousObjectCreationExpression boundAnonymousObjectCreationExpression)
        {
            Lazy<ImmutableArray<IOperation>> memberInitializers = new Lazy<ImmutableArray<IOperation>>(() => GetAnonymousObjectCreationInitializers(boundAnonymousObjectCreationExpression));
            SyntaxNode syntax = boundAnonymousObjectCreationExpression.Syntax;
            ITypeSymbol type = boundAnonymousObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundAnonymousObjectCreationExpression.ConstantValue);
            bool isImplicit = boundAnonymousObjectCreationExpression.WasCompilerGenerated;
            return new LazyAnonymousObjectCreationExpression(memberInitializers, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IPropertyReferenceOperation CreateBoundAnonymousPropertyDeclarationOperation(BoundAnonymousPropertyDeclaration boundAnonymousPropertyDeclaration)
        {
            PropertySymbol property = boundAnonymousPropertyDeclaration.Property;
            Lazy<IOperation> instance = new Lazy<IOperation>(() => null);
            Lazy<ImmutableArray<IArgumentOperation>> arguments = new Lazy<ImmutableArray<IArgumentOperation>>(() => ImmutableArray<IArgumentOperation>.Empty);
            SyntaxNode syntax = boundAnonymousPropertyDeclaration.Syntax;
            ITypeSymbol type = boundAnonymousPropertyDeclaration.Type;
            Optional<object> constantValue = ConvertToOptional(boundAnonymousPropertyDeclaration.ConstantValue);
            bool isImplicit = boundAnonymousPropertyDeclaration.WasCompilerGenerated;
            return new LazyPropertyReferenceExpression(property, instance, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundObjectCreationExpressionOperation(BoundObjectCreationExpression boundObjectCreationExpression)
        {
            MethodSymbol constructor = boundObjectCreationExpression.Constructor;
            SyntaxNode syntax = boundObjectCreationExpression.Syntax;
            ITypeSymbol type = boundObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectCreationExpression.ConstantValue);
            bool isImplicit = boundObjectCreationExpression.WasCompilerGenerated;
            
            if (boundObjectCreationExpression.ResultKind == LookupResultKind.OverloadResolutionFailure || constructor == null || constructor.OriginalDefinition is ErrorMethodSymbol)
            {
                return CreateInvalidExpressionForHasArgumentsExpression(null, boundObjectCreationExpression.Arguments, boundObjectCreationExpression.InitializerExpressionOpt, syntax, type, constantValue, isImplicit);
            }

            Lazy<IObjectOrCollectionInitializerOperation> initializer = new Lazy<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(boundObjectCreationExpression.InitializerExpressionOpt));
            Lazy<ImmutableArray<IArgumentOperation>> arguments = new Lazy<ImmutableArray<IArgumentOperation>>(() =>
            {
                return DeriveArguments(
                    boundObjectCreationExpression,
                    boundObjectCreationExpression.BinderOpt,
                    constructor,
                    constructor,
                    boundObjectCreationExpression.Arguments,
                    boundObjectCreationExpression.ArgumentNamesOpt,
                    boundObjectCreationExpression.ArgsToParamsOpt,
                    boundObjectCreationExpression.ArgumentRefKindsOpt,
                    constructor.Parameters,
                    boundObjectCreationExpression.Expanded,
                    syntax);
            });
            return new LazyObjectCreationExpression(constructor, initializer, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicObjectCreationOperation CreateBoundDynamicObjectCreationExpressionOperation(BoundDynamicObjectCreationExpression boundDynamicObjectCreationExpression)
        {
            Lazy<ImmutableArray<IOperation>> arguments = new Lazy<ImmutableArray<IOperation>>(() => boundDynamicObjectCreationExpression.Arguments.SelectAsArray(n => Create(n)));
            ImmutableArray<string> argumentNames = boundDynamicObjectCreationExpression.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicObjectCreationExpression.ArgumentRefKindsOpt.NullToEmpty();
            Lazy<IObjectOrCollectionInitializerOperation> initializer = new Lazy<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(boundDynamicObjectCreationExpression.InitializerExpressionOpt));
            SyntaxNode syntax = boundDynamicObjectCreationExpression.Syntax;
            ITypeSymbol type = boundDynamicObjectCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicObjectCreationExpression.ConstantValue);
            bool isImplicit = boundDynamicObjectCreationExpression.WasCompilerGenerated;
            return new LazyDynamicObjectCreationExpression(arguments, argumentNames, argumentRefKinds, initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicInvocationOperation CreateBoundDynamicInvocationExpressionOperation(BoundDynamicInvocation boundDynamicInvocation)
        {
            Lazy<IOperation> expression;
            if (boundDynamicInvocation.Expression.Kind == BoundKind.MethodGroup)
            {
                var methodGroup = (BoundMethodGroup)boundDynamicInvocation.Expression;
                expression = new Lazy<IOperation>(() => CreateBoundDynamicMemberAccessOperation(methodGroup.ReceiverOpt, methodGroup.TypeArgumentsOpt,
                    methodGroup.Name, methodGroup.Syntax, methodGroup.Type, methodGroup.ConstantValue, methodGroup.WasCompilerGenerated));
            }
            else
            {
                expression = new Lazy<IOperation>(() => Create(boundDynamicInvocation.Expression));
            }
            Lazy<ImmutableArray<IOperation>> arguments = new Lazy<ImmutableArray<IOperation>>(() => boundDynamicInvocation.Arguments.SelectAsArray(n => Create(n)));
            ImmutableArray<string> argumentNames = boundDynamicInvocation.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicInvocation.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicInvocation.Syntax;
            ITypeSymbol type = boundDynamicInvocation.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicInvocation.ConstantValue);
            bool isImplicit = boundDynamicInvocation.WasCompilerGenerated;
            return new LazyDynamicInvocationExpression(expression, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicIndexerAccessOperation CreateBoundDynamicIndexerAccessExpressionOperation(BoundDynamicIndexerAccess boundDynamicIndexerAccess)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundDynamicIndexerAccess.ReceiverOpt));
            Lazy<ImmutableArray<IOperation>> arguments = new Lazy<ImmutableArray<IOperation>>(() => boundDynamicIndexerAccess.Arguments.SelectAsArray(n => Create(n)));
            ImmutableArray<string> argumentNames = boundDynamicIndexerAccess.ArgumentNamesOpt.NullToEmpty();
            ImmutableArray<RefKind> argumentRefKinds = boundDynamicIndexerAccess.ArgumentRefKindsOpt.NullToEmpty();
            SyntaxNode syntax = boundDynamicIndexerAccess.Syntax;
            ITypeSymbol type = boundDynamicIndexerAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundDynamicIndexerAccess.ConstantValue);
            bool isImplicit = boundDynamicIndexerAccess.WasCompilerGenerated;
            return new LazyDynamicIndexerAccessExpression(expression, arguments, argumentNames, argumentRefKinds, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IObjectOrCollectionInitializerOperation CreateBoundObjectInitializerExpressionOperation(BoundObjectInitializerExpression boundObjectInitializerExpression)
        {
            Lazy<ImmutableArray<IOperation>> initializers = new Lazy<ImmutableArray<IOperation>>(() => BoundObjectCreationExpression.GetChildInitializers(boundObjectInitializerExpression).SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundObjectInitializerExpression.Syntax;
            ITypeSymbol type = boundObjectInitializerExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectInitializerExpression.ConstantValue);
            bool isImplicit = boundObjectInitializerExpression.WasCompilerGenerated;
            return new LazyObjectOrCollectionInitializerExpression(initializers, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IObjectOrCollectionInitializerOperation CreateBoundCollectionInitializerExpressionOperation(BoundCollectionInitializerExpression boundCollectionInitializerExpression)
        {
            Lazy<ImmutableArray<IOperation>> initializers = new Lazy<ImmutableArray<IOperation>>(() => BoundObjectCreationExpression.GetChildInitializers(boundCollectionInitializerExpression).SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundCollectionInitializerExpression.Syntax;
            ITypeSymbol type = boundCollectionInitializerExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionInitializerExpression.ConstantValue);
            bool isImplicit = boundCollectionInitializerExpression.WasCompilerGenerated;
            return new LazyObjectOrCollectionInitializerExpression(initializers, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundObjectInitializerMemberOperation(BoundObjectInitializerMember boundObjectInitializerMember)
        {
            Lazy<IOperation> instance = new Lazy<IOperation>(() => new InstanceReferenceExpression(
                    semanticModel: _semanticModel,
                    syntax: boundObjectInitializerMember.Syntax,
                    type: boundObjectInitializerMember.ReceiverType,
                    constantValue: default(Optional<object>),
                    isImplicit: true));

            SyntaxNode syntax = boundObjectInitializerMember.Syntax;
            ITypeSymbol type = boundObjectInitializerMember.Type;
            Optional<object> constantValue = ConvertToOptional(boundObjectInitializerMember.ConstantValue);
            bool isImplicit = boundObjectInitializerMember.WasCompilerGenerated;

            switch (boundObjectInitializerMember.MemberSymbol.Kind)
            {
                case SymbolKind.Field:
                    var field = (FieldSymbol)boundObjectInitializerMember.MemberSymbol;
                    bool isDeclaration = false;
                    return new LazyFieldReferenceExpression(field, isDeclaration, instance, _semanticModel, syntax, type, constantValue, isImplicit);
                case SymbolKind.Event:
                    var eventSymbol = (EventSymbol)boundObjectInitializerMember.MemberSymbol;
                    return new LazyEventReferenceExpression(eventSymbol, instance, _semanticModel, syntax, type, constantValue, isImplicit);
                case SymbolKind.Property:
                    var property = (PropertySymbol)boundObjectInitializerMember.MemberSymbol;
                    Lazy<ImmutableArray<IArgumentOperation>> arguments;
                    if (!boundObjectInitializerMember.Arguments.Any())
                    {
                        // Simple property reference.
                        arguments = new Lazy<ImmutableArray<IArgumentOperation>>(() => ImmutableArray<IArgumentOperation>.Empty);
                    }
                    else
                    {
                        var accessor = property.GetOwnOrInheritedSetMethod();
                        if (accessor == null || boundObjectInitializerMember.ResultKind == LookupResultKind.OverloadResolutionFailure || accessor.OriginalDefinition is ErrorMethodSymbol)
                        {
                            Lazy<ImmutableArray<IOperation>> children = new Lazy<ImmutableArray<IOperation>>(() =>
                                boundObjectInitializerMember.Arguments.SelectAsArray(a => Create(a)));
                            return new LazyInvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
                        }
                        // Indexed property reference.
                        arguments = new Lazy<ImmutableArray<IArgumentOperation>>(() =>
                        {
                            return DeriveArguments(
                                boundObjectInitializerMember,
                                boundObjectInitializerMember.BinderOpt,
                                property,
                                accessor,
                                boundObjectInitializerMember.Arguments,
                                boundObjectInitializerMember.ArgumentNamesOpt,
                                boundObjectInitializerMember.ArgsToParamsOpt,
                                boundObjectInitializerMember.ArgumentRefKindsOpt,
                                property.Parameters,
                                boundObjectInitializerMember.Expanded,
                                boundObjectInitializerMember.Syntax);
                        });
                    }

                    return new LazyPropertyReferenceExpression(property, instance, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private ICollectionElementInitializerOperation CreateBoundCollectionElementInitializerOperation(BoundCollectionElementInitializer boundCollectionElementInitializer)
        {
            IMethodSymbol addMethod = boundCollectionElementInitializer.AddMethod;
            Lazy<ImmutableArray<IOperation>> arguments = new Lazy<ImmutableArray<IOperation>>(() => boundCollectionElementInitializer.Arguments.SelectAsArray(n => Create(n)));
            bool isDynamic = false;
            SyntaxNode syntax = boundCollectionElementInitializer.Syntax;
            ITypeSymbol type = boundCollectionElementInitializer.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionElementInitializer.ConstantValue);
            bool isImplicit = boundCollectionElementInitializer.WasCompilerGenerated;
            return new LazyCollectionElementInitializerExpression(addMethod, isDynamic, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDynamicMemberReferenceOperation CreateBoundDynamicMemberAccessOperation(BoundDynamicMemberAccess boundDynamicMemberAccess)
        {
            return CreateBoundDynamicMemberAccessOperation(boundDynamicMemberAccess.Receiver, boundDynamicMemberAccess.TypeArgumentsOpt, boundDynamicMemberAccess.Name,
                boundDynamicMemberAccess.Syntax, boundDynamicMemberAccess.Type, boundDynamicMemberAccess.ConstantValue, boundDynamicMemberAccess.WasCompilerGenerated);
        }

        private IDynamicMemberReferenceOperation CreateBoundDynamicMemberAccessOperation(
            BoundExpression receiver,
            ImmutableArray<TypeSymbol> typeArgumentsOpt,
            string memberName,
            SyntaxNode syntaxNode,
            ITypeSymbol type,
            ConstantValue value,
            bool isImplicit)
        {
            Lazy<IOperation> instance = new Lazy<IOperation>(() => Create(receiver));
            ImmutableArray<ITypeSymbol> typeArguments = ImmutableArray<ITypeSymbol>.Empty;
            if (!typeArgumentsOpt.IsDefault)
            {
                typeArguments = ImmutableArray<ITypeSymbol>.CastUp(typeArgumentsOpt);
            }
            ITypeSymbol containingType = null;
            Optional<object> constantValue = ConvertToOptional(value);
            return new LazyDynamicMemberReferenceExpression(instance, memberName, typeArguments, containingType, _semanticModel, syntaxNode, type, constantValue, isImplicit);
        }

        private ICollectionElementInitializerOperation CreateBoundDynamicCollectionElementInitializerOperation(BoundDynamicCollectionElementInitializer boundCollectionElementInitializer)
        {
            IMethodSymbol addMethod = null;
            Lazy<ImmutableArray<IOperation>> arguments = new Lazy<ImmutableArray<IOperation>>(() => boundCollectionElementInitializer.Arguments.SelectAsArray(n => Create(n)));
            bool isDynamic = true;
            SyntaxNode syntax = boundCollectionElementInitializer.Syntax;
            ITypeSymbol type = boundCollectionElementInitializer.Type;
            Optional<object> constantValue = ConvertToOptional(boundCollectionElementInitializer.ConstantValue);
            bool isImplicit = boundCollectionElementInitializer.WasCompilerGenerated;
            return new LazyCollectionElementInitializerExpression(addMethod, isDynamic, arguments, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateUnboundLambdaOperation(UnboundLambda unboundLambda)
        {
            // We want to ensure that we never see the UnboundLambda node, and that we don't end up having two different IOperation
            // nodes for the lambda expression. So, we ask the semantic model for the IOperation node for the unbound lambda syntax.
            // We are counting on the fact that will do the error recovery and actually create the BoundLambda node appropriate for
            // this syntax node.
            BoundLambda boundLambda = unboundLambda.BindForErrorRecovery();
            return CreateInternal(boundLambda);
        }

        private IAnonymousFunctionOperation CreateBoundLambdaOperation(BoundLambda boundLambda)
        {
            IMethodSymbol symbol = boundLambda.Symbol;
            Lazy<IBlockOperation> body = new Lazy<IBlockOperation>(() => (IBlockOperation)Create(boundLambda.Body));
            SyntaxNode syntax = boundLambda.Syntax;
            // This matches the SemanticModel implementation. This is because in VB, lambdas by themselves
            // do not have a type. To get the type of a lambda expression in the SemanticModel, you need to look at
            // TypeInfo.ConvertedType, rather than TypeInfo.Type. We replicate that behavior here. To get the type of
            // an IAnonymousFunctionExpression, you need to look at the parent IConversionExpression.
            ITypeSymbol type = null;
            Optional<object> constantValue = ConvertToOptional(boundLambda.ConstantValue);
            bool isImplicit = boundLambda.WasCompilerGenerated;
            return new LazyAnonymousFunctionExpression(symbol, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILocalFunctionOperation CreateBoundLocalFunctionStatementOperation(BoundLocalFunctionStatement boundLocalFunctionStatement)
        {
            IMethodSymbol symbol = boundLocalFunctionStatement.Symbol;
            Lazy<IBlockOperation> body = new Lazy<IBlockOperation>(() => (IBlockOperation)Create(boundLocalFunctionStatement.Body));
            SyntaxNode syntax = boundLocalFunctionStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLocalFunctionStatement.WasCompilerGenerated;
            return new LazyLocalFunctionStatement(symbol, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundConversionOperation(BoundConversion boundConversion)
        {
            bool isImplicit = boundConversion.WasCompilerGenerated || !boundConversion.ExplicitCastInCode;
            if (boundConversion.ConversionKind == CSharp.ConversionKind.MethodGroup)
            {
                // We don't check HasErrors on the conversion here because if we actually have a MethodGroup conversion,
                // overload resolution succeeded. The resulting method could be invalid for other reasons, but we don't
                // hide the resolved method.
                Lazy<IOperation> target = new Lazy<IOperation>(() =>
                        CreateBoundMethodGroupSingleMethodOperation((BoundMethodGroup)boundConversion.Operand,
                                                                    boundConversion.SymbolOpt,
                                                                    boundConversion.SuppressVirtualCalls));
                SyntaxNode syntax = boundConversion.Syntax;
                ITypeSymbol type = boundConversion.Type;
                Optional<object> constantValue = ConvertToOptional(boundConversion.ConstantValue);
                return new LazyDelegateCreationExpression(target, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else
            {
                SyntaxNode syntax = boundConversion.Syntax;

                if (syntax.IsMissing)
                {
                    // If the underlying syntax IsMissing, then that means we're in case where the compiler generated a piece of syntax to fill in for
                    // an error, such as this case:
                    //
                    //  int i = ;
                    //
                    // Semantic model has a special case here that we match: if the underlying syntax is missing, don't create a conversion expression,
                    // and instead directly return the operand, which will be a BoundBadExpression. When we generate a node for the BoundBadExpression,
                    // the resulting IOperation will also have a null Type.
                    Debug.Assert(boundConversion.Operand.Kind == BoundKind.BadExpression);
                    return Create(boundConversion.Operand);
                }

                Lazy<IOperation> operand = new Lazy<IOperation>(() => Create(boundConversion.Operand));
                ITypeSymbol type = boundConversion.Type;
                Optional<object> constantValue = ConvertToOptional(boundConversion.ConstantValue);

                // If this is a lambda or method group conversion to a delegate type, we return a delegate creation instead of a conversion
                if ((boundConversion.Operand.Kind == BoundKind.Lambda ||
                     boundConversion.Operand.Kind == BoundKind.UnboundLambda ||
                     boundConversion.Operand.Kind == BoundKind.MethodGroup) &&
                    boundConversion.Type.IsDelegateType())
                {
                    return new LazyDelegateCreationExpression(operand, _semanticModel, syntax, type, constantValue, isImplicit);
                }
                else
                {
                    Conversion conversion = boundConversion.Conversion;
                    bool isTryCast = false;
                    // Checked conversions only matter if the conversion is a Numeric conversion. Don't have true unless the conversion is actually numeric.
                    bool isChecked = conversion.IsNumeric && boundConversion.Checked;
                    return new LazyCSharpConversionExpression(operand, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit);
                }
            }
        }

        private IConversionOperation CreateBoundAsOperatorOperation(BoundAsOperator boundAsOperator)
        {
            Lazy<IOperation> operand = new Lazy<IOperation>(() => Create(boundAsOperator.Operand));
            SyntaxNode syntax = boundAsOperator.Syntax;
            Conversion conversion = boundAsOperator.Conversion;
            bool isTryCast = true;
            bool isChecked = false;
            ITypeSymbol type = boundAsOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAsOperator.ConstantValue);
            bool isImplicit = boundAsOperator.WasCompilerGenerated;
            return new LazyCSharpConversionExpression(operand, conversion, isTryCast, isChecked, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDelegateCreationOperation CreateBoundDelegateCreationExpressionOperation(BoundDelegateCreationExpression boundDelegateCreationExpression)
        {
            Lazy<IOperation> target = new Lazy<IOperation>(() =>
            {
                if (boundDelegateCreationExpression.Argument.Kind == BoundKind.MethodGroup &&
                    boundDelegateCreationExpression.MethodOpt != null)
                {
                    // If this is a method binding, and a valid candidate method was found, then we want to expose
                    // this child as an IMethodBindingReference. Otherwise, we want to just delegate to the standard
                    // CSharpOperationFactory behavior. Note we don't check HasErrors here because if we have a method group,
                    // overload resolution succeeded, even if the resulting method isn't valid for some other reason.
                    BoundMethodGroup boundMethodGroup = (BoundMethodGroup)boundDelegateCreationExpression.Argument;
                    return CreateBoundMethodGroupSingleMethodOperation(boundMethodGroup, boundDelegateCreationExpression.MethodOpt, boundMethodGroup.SuppressVirtualCalls);
                }
                else
                {
                    return Create(boundDelegateCreationExpression.Argument);
                }
            });
            SyntaxNode syntax = boundDelegateCreationExpression.Syntax;
            ITypeSymbol type = boundDelegateCreationExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDelegateCreationExpression.ConstantValue);
            bool isImplicit = boundDelegateCreationExpression.WasCompilerGenerated;
            return new LazyDelegateCreationExpression(target, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IMethodReferenceOperation CreateBoundMethodGroupSingleMethodOperation(BoundMethodGroup boundMethodGroup, IMethodSymbol methodSymbol, bool suppressVirtualCalls)
        {
            bool isVirtual = (methodSymbol.IsAbstract || methodSymbol.IsOverride || methodSymbol.IsVirtual) && !suppressVirtualCalls;
            BoundExpression receiverOpt = boundMethodGroup.InstanceOpt;

            if (methodSymbol.IsStatic && receiverOpt != null && 
                receiverOpt.WasCompilerGenerated && receiverOpt.Kind == BoundKind.ThisReference)
            {
                receiverOpt = null;
            }

            Lazy<IOperation> instance = new Lazy<IOperation>(() => Create(receiverOpt));
            SyntaxNode bindingSyntax = boundMethodGroup.Syntax;
            ITypeSymbol bindingType = null;
            Optional<object> bindingConstantValue = ConvertToOptional(boundMethodGroup.ConstantValue);
            bool isImplicit = boundMethodGroup.WasCompilerGenerated;
            return new LazyMethodReferenceExpression(methodSymbol, isVirtual, instance, _semanticModel, bindingSyntax, bindingType, bindingConstantValue, boundMethodGroup.WasCompilerGenerated);
        }

        private IIsTypeOperation CreateBoundIsOperatorOperation(BoundIsOperator boundIsOperator)
        {
            Lazy<IOperation> valueOperand = new Lazy<IOperation>(() => Create(boundIsOperator.Operand));
            ITypeSymbol typeOperand = boundIsOperator.TargetType.Type;
            SyntaxNode syntax = boundIsOperator.Syntax;
            ITypeSymbol type = boundIsOperator.Type;
            bool isNegated = false;
            Optional<object> constantValue = ConvertToOptional(boundIsOperator.ConstantValue);
            bool isImplicit = boundIsOperator.WasCompilerGenerated;
            return new LazyIsTypeExpression(valueOperand, typeOperand, isNegated, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ISizeOfOperation CreateBoundSizeOfOperatorOperation(BoundSizeOfOperator boundSizeOfOperator)
        {
            ITypeSymbol typeOperand = boundSizeOfOperator.SourceType.Type;
            SyntaxNode syntax = boundSizeOfOperator.Syntax;
            ITypeSymbol type = boundSizeOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundSizeOfOperator.ConstantValue);
            bool isImplicit = boundSizeOfOperator.WasCompilerGenerated;
            return new SizeOfExpression(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITypeOfOperation CreateBoundTypeOfOperatorOperation(BoundTypeOfOperator boundTypeOfOperator)
        {
            ITypeSymbol typeOperand = boundTypeOfOperator.SourceType.Type;
            SyntaxNode syntax = boundTypeOfOperator.Syntax;
            ITypeSymbol type = boundTypeOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundTypeOfOperator.ConstantValue);
            bool isImplicit = boundTypeOfOperator.WasCompilerGenerated;
            return new TypeOfExpression(typeOperand, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayCreationOperation CreateBoundArrayCreationOperation(BoundArrayCreation boundArrayCreation)
        {
            Lazy<ImmutableArray<IOperation>> dimensionSizes = new Lazy<ImmutableArray<IOperation>>(() => boundArrayCreation.Bounds.SelectAsArray(n => Create(n)));
            Lazy<IArrayInitializerOperation> initializer = new Lazy<IArrayInitializerOperation>(() => (IArrayInitializerOperation)Create(boundArrayCreation.InitializerOpt));
            SyntaxNode syntax = boundArrayCreation.Syntax;
            ITypeSymbol type = boundArrayCreation.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayCreation.ConstantValue);
            bool isImplicit = boundArrayCreation.WasCompilerGenerated;
            return new LazyArrayCreationExpression(dimensionSizes, initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayInitializerOperation CreateBoundArrayInitializationOperation(BoundArrayInitialization boundArrayInitialization)
        {
            Lazy<ImmutableArray<IOperation>> elementValues = new Lazy<ImmutableArray<IOperation>>(() => boundArrayInitialization.Initializers.SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundArrayInitialization.Syntax;
            ITypeSymbol type = boundArrayInitialization.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayInitialization.ConstantValue);
            bool isImplicit = boundArrayInitialization.WasCompilerGenerated;
            return new LazyArrayInitializer(elementValues, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDefaultValueOperation CreateBoundDefaultExpressionOperation(BoundDefaultExpression boundDefaultExpression)
        {
            SyntaxNode syntax = boundDefaultExpression.Syntax;
            ITypeSymbol type = boundDefaultExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundDefaultExpression.ConstantValue);
            bool isImplicit = boundDefaultExpression.WasCompilerGenerated;
            return new DefaultValueExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundBaseReferenceOperation(BoundBaseReference boundBaseReference)
        {
            SyntaxNode syntax = boundBaseReference.Syntax;
            ITypeSymbol type = boundBaseReference.Type;
            Optional<object> constantValue = ConvertToOptional(boundBaseReference.ConstantValue);
            bool isImplicit = boundBaseReference.WasCompilerGenerated;
            return new InstanceReferenceExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundThisReferenceOperation(BoundThisReference boundThisReference)
        {
            SyntaxNode syntax = boundThisReference.Syntax;
            ITypeSymbol type = boundThisReference.Type;
            Optional<object> constantValue = ConvertToOptional(boundThisReference.ConstantValue);
            bool isImplicit = boundThisReference.WasCompilerGenerated;
            return new InstanceReferenceExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundAssignmentOperatorOrMemberInitializerOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            return IsMemberInitializer(boundAssignmentOperator) ?
                (IOperation)CreateBoundMemberInitializerOperation(boundAssignmentOperator) :
                CreateBoundAssignmentOperatorOperation(boundAssignmentOperator);
        }

        private static bool IsMemberInitializer(BoundAssignmentOperator boundAssignmentOperator) =>
            boundAssignmentOperator.Right?.Kind == BoundKind.ObjectInitializerExpression || boundAssignmentOperator.Right?.Kind == BoundKind.CollectionInitializerExpression;

        private ISimpleAssignmentOperation CreateBoundAssignmentOperatorOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            Debug.Assert(!IsMemberInitializer(boundAssignmentOperator));

            Lazy<IOperation> target = new Lazy<IOperation>(() => Create(boundAssignmentOperator.Left));
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundAssignmentOperator.Right));
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol type = boundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAssignmentOperator.ConstantValue);
            bool isImplicit = boundAssignmentOperator.WasCompilerGenerated;
            return new LazySimpleAssignmentExpression(target, value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IMemberInitializerOperation CreateBoundMemberInitializerOperation(BoundAssignmentOperator boundAssignmentOperator)
        {
            Debug.Assert(IsMemberInitializer(boundAssignmentOperator));

            Lazy<IMemberReferenceOperation> target = new Lazy<IMemberReferenceOperation>(() => (IMemberReferenceOperation)Create(boundAssignmentOperator.Left));
            Lazy<IObjectOrCollectionInitializerOperation> value = new Lazy<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(boundAssignmentOperator.Right));
            SyntaxNode syntax = boundAssignmentOperator.Syntax;
            ITypeSymbol type = boundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAssignmentOperator.ConstantValue);
            bool isImplicit = boundAssignmentOperator.WasCompilerGenerated;
            return new LazyMemberInitializerExpression(target, value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICompoundAssignmentOperation CreateBoundCompoundAssignmentOperatorOperation(BoundCompoundAssignmentOperator boundCompoundAssignmentOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundCompoundAssignmentOperator.Operator.Kind);
            Lazy<IOperation> target = new Lazy<IOperation>(() => Create(boundCompoundAssignmentOperator.Left));
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundCompoundAssignmentOperator.Right));
            bool isLifted = boundCompoundAssignmentOperator.Operator.Kind.IsLifted();
            bool isChecked = boundCompoundAssignmentOperator.Operator.Kind.IsChecked();
            IMethodSymbol operatorMethod = boundCompoundAssignmentOperator.Operator.Method;
            SyntaxNode syntax = boundCompoundAssignmentOperator.Syntax;
            ITypeSymbol type = boundCompoundAssignmentOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundCompoundAssignmentOperator.ConstantValue);
            bool isImplicit = boundCompoundAssignmentOperator.WasCompilerGenerated;
            return new LazyCompoundAssignmentExpression(operatorKind, isLifted, isChecked, target, value, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IIncrementOrDecrementOperation CreateBoundIncrementOperatorOperation(BoundIncrementOperator boundIncrementOperator)
        {
            bool isDecrement = Helper.IsDecrement(boundIncrementOperator.OperatorKind);
            bool isPostfix = Helper.IsPostfixIncrementOrDecrement(boundIncrementOperator.OperatorKind);
            bool isLifted = boundIncrementOperator.OperatorKind.IsLifted();
            bool isChecked = boundIncrementOperator.OperatorKind.IsChecked();
            Lazy<IOperation> target = new Lazy<IOperation>(() => Create(boundIncrementOperator.Operand));
            IMethodSymbol operatorMethod = boundIncrementOperator.MethodOpt;
            SyntaxNode syntax = boundIncrementOperator.Syntax;
            ITypeSymbol type = boundIncrementOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundIncrementOperator.ConstantValue);
            bool isImplicit = boundIncrementOperator.WasCompilerGenerated;
            return new LazyIncrementExpression(isDecrement, isPostfix, isLifted, isChecked, target, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInvalidOperation CreateBoundBadExpressionOperation(BoundBadExpression boundBadExpression)
        {
            Lazy<ImmutableArray<IOperation>> children = new Lazy<ImmutableArray<IOperation>>(() => boundBadExpression.ChildBoundNodes.Select(n => Create(n)).WhereNotNull().ToImmutableArray());
            SyntaxNode syntax = boundBadExpression.Syntax;
            // We match semantic model here: if the expression IsMissing, we have a null type, rather than the ErrorType of the bound node.
            ITypeSymbol type = syntax.IsMissing ? null : boundBadExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundBadExpression.ConstantValue);

            // if child has syntax node point to same syntax node as bad expression, then this invalid expression is implicit
            bool isImplicit = boundBadExpression.WasCompilerGenerated || boundBadExpression.ChildBoundNodes.Any(e => e?.Syntax == boundBadExpression.Syntax);
            return new LazyInvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ITypeParameterObjectCreationOperation CreateBoundNewTOperation(BoundNewT boundNewT)
        {
            Lazy<IObjectOrCollectionInitializerOperation> initializer = new Lazy<IObjectOrCollectionInitializerOperation>(() => (IObjectOrCollectionInitializerOperation)Create(boundNewT.InitializerExpressionOpt));
            SyntaxNode syntax = boundNewT.Syntax;
            ITypeSymbol type = boundNewT.Type;
            Optional<object> constantValue = ConvertToOptional(boundNewT.ConstantValue);
            bool isImplicit = boundNewT.WasCompilerGenerated;
            return new LazyTypeParameterObjectCreationExpression(initializer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IUnaryOperation CreateBoundUnaryOperatorOperation(BoundUnaryOperator boundUnaryOperator)
        {
            UnaryOperatorKind unaryOperatorKind = Helper.DeriveUnaryOperatorKind(boundUnaryOperator.OperatorKind);
            Lazy<IOperation> operand = new Lazy<IOperation>(() => Create(boundUnaryOperator.Operand));
            IMethodSymbol operatorMethod = boundUnaryOperator.MethodOpt;
            SyntaxNode syntax = boundUnaryOperator.Syntax;
            ITypeSymbol type = boundUnaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundUnaryOperator.ConstantValue);
            bool isLifted = boundUnaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundUnaryOperator.OperatorKind.IsChecked();
            bool isImplicit = boundUnaryOperator.WasCompilerGenerated;
            return new LazyUnaryOperatorExpression(unaryOperatorKind, operand, isLifted, isChecked, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBinaryOperation CreateBoundBinaryOperatorOperation(BoundBinaryOperator boundBinaryOperator)
        {
            BinaryOperatorKind operatorKind = Helper.DeriveBinaryOperatorKind(boundBinaryOperator.OperatorKind);
            Lazy<IOperation> leftOperand = new Lazy<IOperation>(() => Create(boundBinaryOperator.Left));
            Lazy<IOperation> rightOperand = new Lazy<IOperation>(() => Create(boundBinaryOperator.Right));
            IMethodSymbol operatorMethod = boundBinaryOperator.MethodOpt;
            SyntaxNode syntax = boundBinaryOperator.Syntax;
            ITypeSymbol type = boundBinaryOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundBinaryOperator.ConstantValue);
            bool isLifted = boundBinaryOperator.OperatorKind.IsLifted();
            bool isChecked = boundBinaryOperator.OperatorKind.IsChecked();
            bool isCompareText = false;
            bool isImplicit = boundBinaryOperator.WasCompilerGenerated;
            return new LazyBinaryOperatorExpression(operatorKind, leftOperand, rightOperand, isLifted, isChecked, isCompareText, operatorMethod, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalOperation CreateBoundConditionalOperatorOperation(BoundConditionalOperator boundConditionalOperator)
        {
            Lazy<IOperation> condition = new Lazy<IOperation>(() => Create(boundConditionalOperator.Condition));
            Lazy<IOperation> whenTrue = new Lazy<IOperation>(() => Create(boundConditionalOperator.Consequence));
            Lazy<IOperation> whenFalse = new Lazy<IOperation>(() => Create(boundConditionalOperator.Alternative));
            SyntaxNode syntax = boundConditionalOperator.Syntax;
            ITypeSymbol type = boundConditionalOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalOperator.ConstantValue);
            bool isImplicit = boundConditionalOperator.WasCompilerGenerated;
            return new LazyConditionalOperation(condition, whenTrue, whenFalse, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICoalesceOperation CreateBoundNullCoalescingOperatorOperation(BoundNullCoalescingOperator boundNullCoalescingOperator)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundNullCoalescingOperator.LeftOperand));
            Lazy<IOperation> whenNull = new Lazy<IOperation>(() => Create(boundNullCoalescingOperator.RightOperand));
            SyntaxNode syntax = boundNullCoalescingOperator.Syntax;
            ITypeSymbol type = boundNullCoalescingOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundNullCoalescingOperator.ConstantValue);
            bool isImplicit = boundNullCoalescingOperator.WasCompilerGenerated;
            return new LazyCoalesceExpression(expression, whenNull, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAwaitOperation CreateBoundAwaitExpressionOperation(BoundAwaitExpression boundAwaitExpression)
        {
            Lazy<IOperation> awaitedValue = new Lazy<IOperation>(() => Create(boundAwaitExpression.Expression));
            SyntaxNode syntax = boundAwaitExpression.Syntax;
            ITypeSymbol type = boundAwaitExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundAwaitExpression.ConstantValue);
            bool isImplicit = boundAwaitExpression.WasCompilerGenerated;
            return new LazyAwaitExpression(awaitedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IArrayElementReferenceOperation CreateBoundArrayAccessOperation(BoundArrayAccess boundArrayAccess)
        {
            // The compiler will dedupe the boundArrayAccess.Expression between different array references. Some example code:
            //
            // class C
            // {
            //     int[] a;

            //     static void Main()
            //     {
            //         // Compiler dedupes the array access receiver for [0] and [1]
            //         var a = new C { a = { [0] = 1, [1] = 2 } };
            //     }
            // }
            //
            // In order to prevent parent pointer from having an issue with this, we intentionally create a new IOperation node every time
            // we encounter an array access. Since we create from the top down, it should be impossible for us to see the node in
            // boundArrayAccess.Expression before seeing the boundArrayAccess itself, so this should not create any other parent pointer
            // issues.
            Lazy<IOperation> arrayReference = new Lazy<IOperation>(() => CreateInternal(boundArrayAccess.Expression));
            Lazy<ImmutableArray<IOperation>> indices = new Lazy<ImmutableArray<IOperation>>(() => boundArrayAccess.Indices.SelectAsArray(n => Create(n)));
            SyntaxNode syntax = boundArrayAccess.Syntax;
            ITypeSymbol type = boundArrayAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundArrayAccess.ConstantValue);
            bool isImplicit = boundArrayAccess.WasCompilerGenerated;

            return new LazyArrayElementReferenceExpression(arrayReference, indices, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IPointerIndirectionReferenceOperation CreateBoundPointerIndirectionOperatorOperation(BoundPointerIndirectionOperator boundPointerIndirectionOperator)
        {
            Lazy<IOperation> pointer = new Lazy<IOperation>(() => Create(boundPointerIndirectionOperator.Operand));
            SyntaxNode syntax = boundPointerIndirectionOperator.Syntax;
            ITypeSymbol type = boundPointerIndirectionOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundPointerIndirectionOperator.ConstantValue);
            bool isImplicit = boundPointerIndirectionOperator.WasCompilerGenerated;
            return new LazyPointerIndirectionReferenceExpression(pointer, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private INameOfOperation CreateBoundNameOfOperatorOperation(BoundNameOfOperator boundNameOfOperator)
        {
            Lazy<IOperation> argument = new Lazy<IOperation>(() => Create(boundNameOfOperator.Argument));
            SyntaxNode syntax = boundNameOfOperator.Syntax;
            ITypeSymbol type = boundNameOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundNameOfOperator.ConstantValue);
            bool isImplicit = boundNameOfOperator.WasCompilerGenerated;
            return new LazyNameOfExpression(argument, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IThrowOperation CreateBoundThrowExpressionOperation(BoundThrowExpression boundThrowExpression)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundThrowExpression.Expression));
            SyntaxNode syntax = boundThrowExpression.Syntax;
            ITypeSymbol type = boundThrowExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundThrowExpression.ConstantValue);
            bool isImplicit = boundThrowExpression.WasCompilerGenerated;
            return new LazyThrowExpression(expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IAddressOfOperation CreateBoundAddressOfOperatorOperation(BoundAddressOfOperator boundAddressOfOperator)
        {
            Lazy<IOperation> reference = new Lazy<IOperation>(() => Create(boundAddressOfOperator.Operand));
            SyntaxNode syntax = boundAddressOfOperator.Syntax;
            ITypeSymbol type = boundAddressOfOperator.Type;
            Optional<object> constantValue = ConvertToOptional(boundAddressOfOperator.ConstantValue);
            bool isImplicit = boundAddressOfOperator.WasCompilerGenerated;
            return new LazyAddressOfExpression(reference, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInstanceReferenceOperation CreateBoundImplicitReceiverOperation(BoundImplicitReceiver boundImplicitReceiver)
        {
            SyntaxNode syntax = boundImplicitReceiver.Syntax;
            ITypeSymbol type = boundImplicitReceiver.Type;
            Optional<object> constantValue = ConvertToOptional(boundImplicitReceiver.ConstantValue);
            bool isImplicit = boundImplicitReceiver.WasCompilerGenerated;
            return new InstanceReferenceExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalAccessOperation CreateBoundConditionalAccessOperation(BoundConditionalAccess boundConditionalAccess)
        {
            Lazy<IOperation> whenNotNull = new Lazy<IOperation>(() => Create(boundConditionalAccess.AccessExpression));
            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundConditionalAccess.Receiver));
            SyntaxNode syntax = boundConditionalAccess.Syntax;
            ITypeSymbol type = boundConditionalAccess.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalAccess.ConstantValue);
            bool isImplicit = boundConditionalAccess.WasCompilerGenerated;

            return new LazyConditionalAccessExpression(whenNotNull, expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalAccessInstanceOperation CreateBoundConditionalReceiverOperation(BoundConditionalReceiver boundConditionalReceiver)
        {
            SyntaxNode syntax = boundConditionalReceiver.Syntax;
            ITypeSymbol type = boundConditionalReceiver.Type;
            Optional<object> constantValue = ConvertToOptional(boundConditionalReceiver.ConstantValue);
            bool isImplicit = boundConditionalReceiver.WasCompilerGenerated;
            return new ConditionalAccessInstanceExpression(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IFieldInitializerOperation CreateBoundFieldEqualsValueOperation(BoundFieldEqualsValue boundFieldEqualsValue)
        {
            ImmutableArray<IFieldSymbol> initializedFields = ImmutableArray.Create<IFieldSymbol>(boundFieldEqualsValue.Field);
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundFieldEqualsValue.Value));
            OperationKind kind = OperationKind.FieldInitializer;
            SyntaxNode syntax = boundFieldEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundFieldEqualsValue.WasCompilerGenerated;
            return new LazyFieldInitializer(initializedFields, value, kind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IPropertyInitializerOperation CreateBoundPropertyEqualsValueOperation(BoundPropertyEqualsValue boundPropertyEqualsValue)
        {
            IPropertySymbol initializedProperty = boundPropertyEqualsValue.Property;
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundPropertyEqualsValue.Value));
            OperationKind kind = OperationKind.PropertyInitializer;
            SyntaxNode syntax = boundPropertyEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundPropertyEqualsValue.WasCompilerGenerated;
            return new LazyPropertyInitializer(initializedProperty, value, kind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IParameterInitializerOperation CreateBoundParameterEqualsValueOperation(BoundParameterEqualsValue boundParameterEqualsValue)
        {
            IParameterSymbol parameter = boundParameterEqualsValue.Parameter;
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundParameterEqualsValue.Value));
            OperationKind kind = OperationKind.ParameterInitializer;
            SyntaxNode syntax = boundParameterEqualsValue.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundParameterEqualsValue.WasCompilerGenerated;
            return new LazyParameterInitializer(parameter, value, kind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBlockOperation CreateBoundBlockOperation(BoundBlock boundBlock)
        {
            Lazy<ImmutableArray<IOperation>> statements =
                new Lazy<ImmutableArray<IOperation>>(() => boundBlock.Statements.Select(s => (bound: s, operation: Create(s)))
                                                                                // Filter out all OperationKind.None except fixed statements for now.
                                                                                // https://github.com/dotnet/roslyn/issues/21776
                                                                                .Where(s => s.operation.Kind != OperationKind.None ||
                                                                                s.bound.Kind == BoundKind.FixedStatement)
                                                                                .Select(s => s.operation).ToImmutableArray());

            ImmutableArray<ILocalSymbol> locals = boundBlock.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundBlock.WasCompilerGenerated;
            return new LazyBlockStatement(statements, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundContinueStatementOperation(BoundContinueStatement boundContinueStatement)
        {
            ILabelSymbol target = boundContinueStatement.Label;
            BranchKind branchKind = BranchKind.Continue;
            SyntaxNode syntax = boundContinueStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundContinueStatement.WasCompilerGenerated;
            return new BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundBreakStatementOperation(BoundBreakStatement boundBreakStatement)
        {
            ILabelSymbol target = boundBreakStatement.Label;
            BranchKind branchKind = BranchKind.Break;
            SyntaxNode syntax = boundBreakStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundBreakStatement.WasCompilerGenerated;
            return new BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundYieldBreakStatementOperation(BoundYieldBreakStatement boundYieldBreakStatement)
        {
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => Create(null));
            SyntaxNode syntax = boundYieldBreakStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundYieldBreakStatement.WasCompilerGenerated;
            return new LazyReturnStatement(OperationKind.YieldBreak, returnedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IBranchOperation CreateBoundGotoStatementOperation(BoundGotoStatement boundGotoStatement)
        {
            ILabelSymbol target = boundGotoStatement.Label;
            BranchKind branchKind = BranchKind.GoTo;
            SyntaxNode syntax = boundGotoStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundGotoStatement.WasCompilerGenerated;
            return new BranchStatement(target, branchKind, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IEmptyOperation CreateBoundNoOpStatementOperation(BoundNoOpStatement boundNoOpStatement)
        {
            SyntaxNode syntax = boundNoOpStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundNoOpStatement.WasCompilerGenerated;
            return new EmptyStatement(_semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConditionalOperation CreateBoundIfStatementOperation(BoundIfStatement boundIfStatement)
        {
            Lazy<IOperation> condition = new Lazy<IOperation>(() => Create(boundIfStatement.Condition));
            Lazy<IOperation> ifTrueStatement = new Lazy<IOperation>(() => Create(boundIfStatement.Consequence));
            Lazy<IOperation> ifFalseStatement = new Lazy<IOperation>(() => Create(boundIfStatement.AlternativeOpt));
            SyntaxNode syntax = boundIfStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundIfStatement.WasCompilerGenerated;
            return new LazyConditionalOperation(condition, ifTrueStatement, ifFalseStatement, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IWhileLoopOperation CreateBoundWhileStatementOperation(BoundWhileStatement boundWhileStatement)
        {
            Lazy<IOperation> condition = new Lazy<IOperation>(() => Create(boundWhileStatement.Condition));
            Lazy<IOperation> body = new Lazy<IOperation>(() => Create(boundWhileStatement.Body));
            ImmutableArray<ILocalSymbol> locals = boundWhileStatement.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundWhileStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundWhileStatement.WasCompilerGenerated;
            return new LazyWhileLoopStatement(condition, body, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDoLoopOperation CreateBoundDoStatementOperation(BoundDoStatement boundDoStatement)
        {
            DoLoopKind doLoopKind = DoLoopKind.DoWhileBottomLoop;
            Lazy<IOperation> condition = new Lazy<IOperation>(() => Create(boundDoStatement.Condition));
            Lazy<IOperation> body = new Lazy<IOperation>(() => Create(boundDoStatement.Body));
            Lazy<IOperation> ignoredCondition = new Lazy<IOperation>(() => null);
            ImmutableArray<ILocalSymbol> locals = boundDoStatement.Locals.As<ILocalSymbol>();
            SyntaxNode syntax = boundDoStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundDoStatement.WasCompilerGenerated;
            return new LazyDoLoopStatement(doLoopKind, condition, body, ignoredCondition, locals, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IForLoopOperation CreateBoundForStatementOperation(BoundForStatement boundForStatement)
        {
            Lazy<ImmutableArray<IOperation>> before = new Lazy<ImmutableArray<IOperation>>(() => ToStatements(boundForStatement.Initializer));
            Lazy<IOperation> condition = new Lazy<IOperation>(() => Create(boundForStatement.Condition));
            Lazy<ImmutableArray<IOperation>> atLoopBottom = new Lazy<ImmutableArray<IOperation>>(() => ToStatements(boundForStatement.Increment));
            ImmutableArray<ILocalSymbol> locals = boundForStatement.OuterLocals.As<ILocalSymbol>();
            Lazy<IOperation> body = new Lazy<IOperation>(() => Create(boundForStatement.Body));
            SyntaxNode syntax = boundForStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundForStatement.WasCompilerGenerated;
            return new LazyForLoopStatement(before, condition, atLoopBottom, locals, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IForEachLoopOperation CreateBoundForEachStatementOperation(BoundForEachStatement boundForEachStatement)
        {
            ImmutableArray<ILocalSymbol> locals = boundForEachStatement.IterationVariables.As<ILocalSymbol>();
            Lazy<IOperation> loopControlVariable;
            if (boundForEachStatement.DeconstructionOpt != null)
            {
                loopControlVariable = new Lazy<IOperation>(() => Create(boundForEachStatement.DeconstructionOpt.DeconstructionAssignment.Left));
            }
            else if (locals.Length == 1)
            {
                var local = (LocalSymbol)locals.Single();
                bool isDeclaration = true;
                loopControlVariable = new Lazy<IOperation>(() => new LocalReferenceExpression(local, isDeclaration, _semanticModel, local.GetDeclaratorSyntax(), local.Type, local.ConstantValue, local.IsImplicitlyDeclared));
            }
            else
            {
                loopControlVariable = new Lazy<IOperation>(() => null);
            }

            Lazy<IOperation> collection = new Lazy<IOperation>(() => Create(boundForEachStatement.Expression));
            Lazy<IOperation> body = new Lazy<IOperation>(() => Create(boundForEachStatement.Body));
            Lazy<ImmutableArray<IOperation>> nextVariables = new Lazy<ImmutableArray<IOperation>>(() => ImmutableArray<IOperation>.Empty);
            SyntaxNode syntax = boundForEachStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundForEachStatement.WasCompilerGenerated;
            return new LazyForEachLoopStatement(locals, loopControlVariable, collection, nextVariables, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ISwitchOperation CreateBoundSwitchStatementOperation(BoundSwitchStatement boundSwitchStatement)
        {
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundSwitchStatement.Expression));
            Lazy<ImmutableArray<ISwitchCaseOperation>> cases = new Lazy<ImmutableArray<ISwitchCaseOperation>>(() => GetSwitchStatementCases(boundSwitchStatement));
            SyntaxNode syntax = boundSwitchStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundSwitchStatement.WasCompilerGenerated;
            return new LazySwitchStatement(value, cases, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICaseClauseOperation CreateBoundSwitchLabelOperation(BoundSwitchLabel boundSwitchLabel)
        {
            SyntaxNode syntax = boundSwitchLabel.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundSwitchLabel.WasCompilerGenerated;

            if (boundSwitchLabel.ExpressionOpt != null)
            {
                Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundSwitchLabel.ExpressionOpt));
                return new LazySingleValueCaseClause(value, _semanticModel, syntax, type, constantValue, isImplicit);
            }
            else
            {
                return new DefaultCaseClause(_semanticModel, syntax, type, constantValue, isImplicit);
            }
        }

        private ITryOperation CreateBoundTryStatementOperation(BoundTryStatement boundTryStatement)
        {
            Lazy<IBlockOperation> body = new Lazy<IBlockOperation>(() => (IBlockOperation)Create(boundTryStatement.TryBlock));
            Lazy<ImmutableArray<ICatchClauseOperation>> catches = new Lazy<ImmutableArray<ICatchClauseOperation>>(() => boundTryStatement.CatchBlocks.SelectAsArray(n => (ICatchClauseOperation)Create(n)));
            Lazy<IBlockOperation> finallyHandler = new Lazy<IBlockOperation>(() => (IBlockOperation)Create(boundTryStatement.FinallyBlockOpt));
            SyntaxNode syntax = boundTryStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundTryStatement.WasCompilerGenerated;
            return new LazyTryStatement(body, catches, finallyHandler, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICatchClauseOperation CreateBoundCatchBlockOperation(BoundCatchBlock boundCatchBlock)
        {
            var exceptionSourceOpt = (BoundLocal)boundCatchBlock.ExceptionSourceOpt;
            Lazy<IOperation> expressionDeclarationOrExpression = new Lazy<IOperation>(() => exceptionSourceOpt != null ? CreateVariableDeclaration(exceptionSourceOpt) : null);
            ITypeSymbol exceptionType = boundCatchBlock.ExceptionTypeOpt;
            ImmutableArray<ILocalSymbol> locals = boundCatchBlock.Locals.As<ILocalSymbol>();
            Lazy<IOperation> filter = new Lazy<IOperation>(() => Create(boundCatchBlock.ExceptionFilterOpt));
            Lazy<IBlockOperation> handler = new Lazy<IBlockOperation>(() => (IBlockOperation)Create(boundCatchBlock.Body));
            SyntaxNode syntax = boundCatchBlock.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundCatchBlock.WasCompilerGenerated;
            return new LazyCatchClause(expressionDeclarationOrExpression, exceptionType, locals, filter, handler, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IFixedOperation CreateBoundFixedStatementOperation(BoundFixedStatement boundFixedStatement)
        {
            Lazy<IVariableDeclarationsOperation> variables = new Lazy<IVariableDeclarationsOperation>(() => (IVariableDeclarationsOperation)Create(boundFixedStatement.Declarations));
            Lazy<IOperation> body = new Lazy<IOperation>(() => Create(boundFixedStatement.Body));
            SyntaxNode syntax = boundFixedStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundFixedStatement.WasCompilerGenerated;
            return new LazyFixedStatement(variables, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IUsingOperation CreateBoundUsingStatementOperation(BoundUsingStatement boundUsingStatement)
        {
            Lazy<IOperation> resources = new Lazy<IOperation>(() => Create((BoundNode)boundUsingStatement.DeclarationsOpt ?? boundUsingStatement.ExpressionOpt));
            Lazy<IOperation> body = new Lazy<IOperation>(() => Create(boundUsingStatement.Body));
            SyntaxNode syntax = boundUsingStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundUsingStatement.WasCompilerGenerated;
            return new LazyUsingStatement(resources, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IExpressionStatementOperation CreateBoundThrowStatementOperation(BoundThrowStatement boundThrowStatement)
        {
            Lazy<IOperation> thrownObject = new Lazy<IOperation>(() => Create(boundThrowStatement.ExpressionOpt));
            SyntaxNode syntax = boundThrowStatement.Syntax;
            ITypeSymbol throwExpressionType = boundThrowStatement.ExpressionOpt?.Type;
            ITypeSymbol statementType = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundThrowStatement.WasCompilerGenerated;
            IOperation throwExpression = new LazyThrowExpression(thrownObject, _semanticModel, syntax, throwExpressionType, constantValue, isImplicit);
            return new ExpressionStatement(throwExpression, _semanticModel, syntax, statementType, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundReturnStatementOperation(BoundReturnStatement boundReturnStatement)
        {
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => Create(boundReturnStatement.ExpressionOpt));
            SyntaxNode syntax = boundReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundReturnStatement.WasCompilerGenerated;
            return new LazyReturnStatement(OperationKind.Return, returnedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IReturnOperation CreateBoundYieldReturnStatementOperation(BoundYieldReturnStatement boundYieldReturnStatement)
        {
            Lazy<IOperation> returnedValue = new Lazy<IOperation>(() => Create(boundYieldReturnStatement.Expression));
            SyntaxNode syntax = boundYieldReturnStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundYieldReturnStatement.WasCompilerGenerated;
            return new LazyReturnStatement(OperationKind.YieldReturn, returnedValue, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILockOperation CreateBoundLockStatementOperation(BoundLockStatement boundLockStatement)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundLockStatement.Argument));
            Lazy<IOperation> body = new Lazy<IOperation>(() => Create(boundLockStatement.Body));
            SyntaxNode syntax = boundLockStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLockStatement.WasCompilerGenerated;

            return new LazyLockStatement(expression, body, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInvalidOperation CreateBoundBadStatementOperation(BoundBadStatement boundBadStatement)
        {
            Lazy<ImmutableArray<IOperation>> children = new Lazy<ImmutableArray<IOperation>>(() => boundBadStatement.ChildBoundNodes.Select(n => Create(n)).WhereNotNull().ToImmutableArray());
            SyntaxNode syntax = boundBadStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);

            // if child has syntax node point to same syntax node as bad statement, then this invalid statement is implicit
            bool isImplicit = boundBadStatement.WasCompilerGenerated || boundBadStatement.ChildBoundNodes.Any(e => e?.Syntax == boundBadStatement.Syntax);
            return new LazyInvalidOperation(children, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundLocalDeclarationOperation(BoundLocalDeclaration boundLocalDeclaration)
        {
            var node = boundLocalDeclaration.Syntax;
            var kind = node.Kind();

            SyntaxNode varStatement;
            SyntaxNode varDeclaration;
            switch (kind)
            {
                case SyntaxKind.LocalDeclarationStatement:
                {
                    var statement = (LocalDeclarationStatementSyntax)node;

                    // this happen for simple int i = 0;
                    // var statement points to LocalDeclarationStatementSyntax
                    varStatement = statement;

                    // var declaration points to VariableDeclaratorSyntax
                    varDeclaration = statement.Declaration.Variables.First();
                    break;
                }
                case SyntaxKind.VariableDeclarator:
                {
                    // this happen for 'for loop' initializer
                    // var statement points to VariableDeclarationSyntax
                    varStatement = node.Parent;

                    // var declaration points to VariableDeclaratorSyntax
                    varDeclaration = node;
                    break;
                }
                default:
                {
                    Debug.Fail($"Unexpected syntax: {kind}");

                    // otherwise, they points to whatever bound nodes are pointing to.
                    varStatement = varDeclaration = node;
                    break;
                }
            }

            Lazy<ImmutableArray<IVariableDeclarationOperation>> declarations = new Lazy<ImmutableArray<IVariableDeclarationOperation>>(() => ImmutableArray.Create(CreateVariableDeclarationInternal(boundLocalDeclaration, varDeclaration)));
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLocalDeclaration.WasCompilerGenerated;
            return new LazyVariableDeclarationStatement(declarations, _semanticModel, varStatement, type, constantValue, isImplicit);
        }

        private IVariableDeclarationsOperation CreateBoundMultipleLocalDeclarationsOperation(BoundMultipleLocalDeclarations boundMultipleLocalDeclarations)
        {
            Lazy<ImmutableArray<IVariableDeclarationOperation>> declarations = new Lazy<ImmutableArray<IVariableDeclarationOperation>>(() =>
                boundMultipleLocalDeclarations.LocalDeclarations.SelectAsArray(declaration => (IVariableDeclarationOperation)CreateVariableDeclaration(declaration)));
            SyntaxNode syntax = boundMultipleLocalDeclarations.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundMultipleLocalDeclarations.WasCompilerGenerated;
            return new LazyVariableDeclarationStatement(declarations, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILabeledOperation CreateBoundLabelStatementOperation(BoundLabelStatement boundLabelStatement)
        {
            ILabelSymbol label = boundLabelStatement.Label;
            Lazy<IOperation> statement = new Lazy<IOperation>(() => Create(null));
            SyntaxNode syntax = boundLabelStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLabelStatement.WasCompilerGenerated;
            return new LazyLabeledStatement(label, statement, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ILabeledOperation CreateBoundLabeledStatementOperation(BoundLabeledStatement boundLabeledStatement)
        {
            ILabelSymbol label = boundLabeledStatement.Label;
            Lazy<IOperation> labeledStatement = new Lazy<IOperation>(() => Create(boundLabeledStatement.Body));
            SyntaxNode syntax = boundLabeledStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundLabeledStatement.WasCompilerGenerated;
            return new LazyLabeledStatement(label, labeledStatement, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IExpressionStatementOperation CreateBoundExpressionStatementOperation(BoundExpressionStatement boundExpressionStatement)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundExpressionStatement.Expression));
            SyntaxNode syntax = boundExpressionStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);

            // lambda body can point to expression directly and binder can insert exression statement there. and end up statement pointing to
            // expression syntax node since there is no statement syntax node to point to. this will mark such one as implicit since it doesn't
            // actually exist in code
            bool isImplicit = boundExpressionStatement.WasCompilerGenerated || boundExpressionStatement.Syntax == boundExpressionStatement.Expression.Syntax;
            return new LazyExpressionStatement(expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundTupleExpressionOperation(BoundTupleExpression boundTupleExpression)
        {
            Lazy<ImmutableArray<IOperation>> elements = new Lazy<ImmutableArray<IOperation>>(() => boundTupleExpression.Arguments.SelectAsArray(element => Create(element)));
            SyntaxNode syntax = boundTupleExpression.Syntax;
            bool isImplicit = boundTupleExpression.WasCompilerGenerated;
            ITypeSymbol type = boundTupleExpression.Type;
            Optional<object> constantValue = default;
            if (syntax is DeclarationExpressionSyntax declarationExpressionSyntax)
            {
                var tupleSyntax = declarationExpressionSyntax.Designation;
                Lazy<IOperation> tupleExpression = new Lazy<IOperation>(() => new LazyTupleExpression(elements, _semanticModel, tupleSyntax, type, constantValue, isImplicit));
                return new LazyDeclarationExpression(tupleExpression, _semanticModel, declarationExpressionSyntax, type, constantValue: default, isImplicit: false);
            }

            return new LazyTupleExpression(elements, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInterpolatedStringOperation CreateBoundInterpolatedStringExpressionOperation(BoundInterpolatedString boundInterpolatedString)
        {
            Lazy<ImmutableArray<IInterpolatedStringContentOperation>> parts = new Lazy<ImmutableArray<IInterpolatedStringContentOperation>>(() =>
                boundInterpolatedString.Parts.SelectAsArray(interpolatedStringContent => CreateBoundInterpolatedStringContentOperation(interpolatedStringContent)));
            SyntaxNode syntax = boundInterpolatedString.Syntax;
            ITypeSymbol type = boundInterpolatedString.Type;
            Optional<object> constantValue = ConvertToOptional(boundInterpolatedString.ConstantValue);
            bool isImplicit = boundInterpolatedString.WasCompilerGenerated;
            return new LazyInterpolatedStringExpression(parts, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInterpolatedStringContentOperation CreateBoundInterpolatedStringContentOperation(BoundNode boundNode)
        {
            if (boundNode.Kind == BoundKind.StringInsert)
            {
                return (IInterpolatedStringContentOperation)Create(boundNode);
            }
            else
            {
                return CreateBoundInterpolatedStringTextOperation(boundNode);
            }
        }

        private IInterpolationOperation CreateBoundInterpolationOperation(BoundStringInsert boundStringInsert)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundStringInsert.Value));
            Lazy<IOperation> alignment = new Lazy<IOperation>(() => Create(boundStringInsert.Alignment));
            Lazy<IOperation> format = new Lazy<IOperation>(() => Create(boundStringInsert.Format));
            SyntaxNode syntax = boundStringInsert.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundStringInsert.WasCompilerGenerated;
            return new LazyInterpolation(expression, alignment, format, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IInterpolatedStringTextOperation CreateBoundInterpolatedStringTextOperation(BoundNode boundNode)
        {
            Lazy<IOperation> text = new Lazy<IOperation>(() => Create(boundNode));
            SyntaxNode syntax = boundNode.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundNode.WasCompilerGenerated;
            return new LazyInterpolatedStringText(text, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IConstantPatternOperation CreateBoundConstantPatternOperation(BoundConstantPattern boundConstantPattern)
        {
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundConstantPattern.Value));
            SyntaxNode syntax = boundConstantPattern.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundConstantPattern.WasCompilerGenerated;
            return new LazyConstantPattern(value, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IDeclarationPatternOperation CreateBoundDeclarationPatternOperation(BoundDeclarationPattern boundDeclarationPattern)
        {
            ISymbol variable = boundDeclarationPattern.Variable;
            SyntaxNode syntax = boundDeclarationPattern.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundDeclarationPattern.WasCompilerGenerated;
            return new DeclarationPattern(variable, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ISwitchOperation CreateBoundPatternSwitchStatementOperation(BoundPatternSwitchStatement boundPatternSwitchStatement)
        {
            Lazy<IOperation> value = new Lazy<IOperation>(() => Create(boundPatternSwitchStatement.Expression));
            Lazy<ImmutableArray<ISwitchCaseOperation>> cases = new Lazy<ImmutableArray<ISwitchCaseOperation>>(() => GetPatternSwitchStatementCases(boundPatternSwitchStatement));
            SyntaxNode syntax = boundPatternSwitchStatement.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundPatternSwitchStatement.WasCompilerGenerated;
            return new LazySwitchStatement(value, cases, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private ICaseClauseOperation CreateBoundPatternSwitchLabelOperation(BoundPatternSwitchLabel boundPatternSwitchLabel)
        {
            SyntaxNode syntax = boundPatternSwitchLabel.Syntax;
            ITypeSymbol type = null;
            Optional<object> constantValue = default(Optional<object>);
            bool isImplicit = boundPatternSwitchLabel.WasCompilerGenerated;

            if (boundPatternSwitchLabel.Pattern.Kind == BoundKind.WildcardPattern)
            {
                // Default switch label in pattern switch statement is represented as a default case clause.
                return new DefaultCaseClause(_semanticModel, syntax, type, constantValue, isImplicit);
            }
            else
            {
                LabelSymbol label = boundPatternSwitchLabel.Label;
                Lazy<IPatternOperation> pattern = new Lazy<IPatternOperation>(() => (IPatternOperation)Create(boundPatternSwitchLabel.Pattern));
                Lazy<IOperation> guardExpression = new Lazy<IOperation>(() => Create(boundPatternSwitchLabel.Guard));
                return new LazyPatternCaseClause(label, pattern, guardExpression, _semanticModel, syntax, type, constantValue, isImplicit);
            }
        }

        private IIsPatternOperation CreateBoundIsPatternExpressionOperation(BoundIsPatternExpression boundIsPatternExpression)
        {
            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundIsPatternExpression.Expression));
            Lazy<IPatternOperation> pattern = new Lazy<IPatternOperation>(() => (IPatternOperation)Create(boundIsPatternExpression.Pattern));
            SyntaxNode syntax = boundIsPatternExpression.Syntax;
            ITypeSymbol type = boundIsPatternExpression.Type;
            Optional<object> constantValue = ConvertToOptional(boundIsPatternExpression.ConstantValue);
            bool isImplicit = boundIsPatternExpression.WasCompilerGenerated;
            return new LazyIsPatternExpression(expression, pattern, _semanticModel, syntax, type, constantValue, isImplicit);
        }

        private IOperation CreateBoundQueryClauseOperation(BoundQueryClause boundQueryClause)
        {
            if (boundQueryClause.Syntax.Kind() != SyntaxKind.QueryExpression)
            {
                // Currently we have no IOperation APIs for different query clauses or continuation.
                return Create(boundQueryClause.Value);
            }

            Lazy<IOperation> expression = new Lazy<IOperation>(() => Create(boundQueryClause.Value));
            SyntaxNode syntax = boundQueryClause.Syntax;
            ITypeSymbol type = boundQueryClause.Type;
            Optional<object> constantValue = ConvertToOptional(boundQueryClause.ConstantValue);
            bool isImplicit = boundQueryClause.WasCompilerGenerated;
            return new LazyTranslatedQueryExpression(expression, _semanticModel, syntax, type, constantValue, isImplicit);
        }
    }
}
