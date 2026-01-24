using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace SagaFlow.Analyzer;

/// <summary>
///     Analyzer that validates the correct usage of RetryPolicyAttribute.
///     Ensures that:
///     1. Exception types in RetryOnExceptions and NonRetryableExceptions inherit from System.Exception.
///     2. The attribute is only applied to classes implementing ISagaCommandHandler or ISagaEventHandler.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class RetryPolicyAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    ///     Diagnostic ID for RetryOnExceptions validation rule.
    /// </summary>
    public const string RetryOnExceptionsRuleDiagnosticId = "CHSG0001";

    /// <summary>
    ///     Diagnostic ID for NonRetryableExceptions validation rule.
    /// </summary>
    public const string NonRetryableExceptionsRuleDiagnosticId = "CHSG0002";

    /// <summary>
    ///     Diagnostic ID for handler implementation validation rule.
    /// </summary>
    public const string HandlerRequiredRuleDiagnosticId = "CHSG0003";

    private const string RetryPolicyAttributeName = "RetryPolicyAttribute";
    private const string RetryOnExceptionsPropertyName = "RetryOnExceptions";
    private const string NonRetryableExceptionsPropertyName = "NonRetryableExceptions";
    private const string SagaCommandHandlerInterfaceName = "ISagaCommandHandler";
    private const string SagaEventHandlerInterfaceName = "ISagaEventHandler";

    // CHSG0001: RetryOnExceptions types must inherit from Exception
    private static readonly LocalizableString RetryOnExceptionsRuleTitle = new LocalizableResourceString(
        nameof(Resources.CHSG0001Title),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString RetryOnExceptionsRuleMessageFormat = new LocalizableResourceString(
        nameof(Resources.CHSG0001MessageFormat),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString RetryOnExceptionsRuleDescription = new LocalizableResourceString(
        nameof(Resources.CHSG0001Description),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly DiagnosticDescriptor RetryOnExceptionsRule = new(
        RetryOnExceptionsRuleDiagnosticId,
        RetryOnExceptionsRuleTitle,
        RetryOnExceptionsRuleMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        true,
        RetryOnExceptionsRuleDescription);

    // CHSG0002: NonRetryableExceptions types must inherit from Exception
    private static readonly LocalizableString NonRetryableExceptionsRuleTitle = new LocalizableResourceString(
        nameof(Resources.CHSG0002Title),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString NonRetryableExceptionsRuleMessageFormat = new LocalizableResourceString(
        nameof(Resources.CHSG0002MessageFormat),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString NonRetryableExceptionsRuleDescription = new LocalizableResourceString(
        nameof(Resources.CHSG0002Description),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly DiagnosticDescriptor NonRetryableExceptionsRule = new(
        NonRetryableExceptionsRuleDiagnosticId,
        NonRetryableExceptionsRuleTitle,
        NonRetryableExceptionsRuleMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        true,
        NonRetryableExceptionsRuleDescription);

    // CHSG0003: RetryPolicyAttribute requires handler implementation
    private static readonly LocalizableString HandlerRequiredRuleTitle = new LocalizableResourceString(
        nameof(Resources.CHSG0003Title),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString HandlerRequiredRuleMessageFormat = new LocalizableResourceString(
        nameof(Resources.CHSG0003MessageFormat),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString HandlerRequiredRuleDescription = new LocalizableResourceString(
        nameof(Resources.CHSG0003Description),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly DiagnosticDescriptor HandlerRequiredRule = new(
        HandlerRequiredRuleDiagnosticId,
        HandlerRequiredRuleTitle,
        HandlerRequiredRuleMessageFormat,
        "Usage",
        DiagnosticSeverity.Error,
        true,
        HandlerRequiredRuleDescription);

    /// <summary>
    ///     Gets the supported diagnostic descriptors for this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(RetryOnExceptionsRule, NonRetryableExceptionsRule, HandlerRequiredRule);

    /// <summary>
    ///     Initializes the analyzer by registering analysis actions.
    /// </summary>
    /// <param name="context">Analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        // Avoid analyzing generated code.
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        // Enable concurrent execution for better performance.
        context.EnableConcurrentExecution();

        // Register semantic model action to analyze attributes on class declarations.
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    /// <summary>
    ///     Analyzes named types (classes) for RetryPolicyAttribute usage.
    /// </summary>
    /// <param name="context">Symbol analysis context.</param>
    private void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        // Only analyze classes.
        if (namedTypeSymbol.TypeKind != TypeKind.Class)
        {
            return;
        }

        // Get all attributes on the class.
        ImmutableArray<AttributeData> attributes = namedTypeSymbol.GetAttributes();

        foreach (AttributeData? attribute in attributes)
        {
            // Check if this is RetryPolicyAttribute.
            if (attribute.AttributeClass?.Name != RetryPolicyAttributeName)
            {
                continue;
            }

            // Validate that the class implements ISagaCommandHandler<> or ISagaEventHandler<,>.
            ValidateHandlerImplementation(context, namedTypeSymbol, attribute);

            // Validate RetryOnExceptions property.
            ValidateExceptionTypesProperty(
                context,
                attribute,
                RetryOnExceptionsPropertyName,
                RetryOnExceptionsRule);

            // Validate NonRetryableExceptions property.
            ValidateExceptionTypesProperty(
                context,
                attribute,
                NonRetryableExceptionsPropertyName,
                NonRetryableExceptionsRule);
        }
    }

    /// <summary>
    ///     Validates that the class implements ISagaCommandHandler or ISagaEventHandler.
    /// </summary>
    /// <param name="context">Symbol analysis context.</param>
    /// <param name="namedTypeSymbol">The class being analyzed.</param>
    /// <param name="attribute">The RetryPolicyAttribute being validated.</param>
    private static void ValidateHandlerImplementation(
        SymbolAnalysisContext context,
        INamedTypeSymbol namedTypeSymbol,
        AttributeData attribute)
    {
        if (ImplementsHandlerInterface(namedTypeSymbol))
        {
            return;
        }

        Location location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                            ?? namedTypeSymbol.Locations[0];

        var diagnostic = Diagnostic.Create(
            HandlerRequiredRule,
            location,
            namedTypeSymbol.Name);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    ///     Checks if the type implements ISagaCommandHandler or ISagaEventHandler interfaces.
    /// </summary>
    /// <param name="typeSymbol">The type to check.</param>
    /// <returns>True if the type implements a handler interface, false otherwise.</returns>
    private static bool ImplementsHandlerInterface(INamedTypeSymbol typeSymbol)
    {
        foreach (INamedTypeSymbol interfaceSymbol in typeSymbol.AllInterfaces)
        {
            string interfaceName = interfaceSymbol.Name;
            if (interfaceName == SagaCommandHandlerInterfaceName ||
                interfaceName == SagaEventHandlerInterfaceName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Validates that all types in an exception array property inherit from System.Exception.
    /// </summary>
    /// <param name="context">Symbol analysis context.</param>
    /// <param name="attribute">The attribute being analyzed.</param>
    /// <param name="propertyName">The name of the property to validate.</param>
    /// <param name="rule">The diagnostic rule to report if validation fails.</param>
    private static void ValidateExceptionTypesProperty(
        SymbolAnalysisContext context,
        AttributeData attribute,
        string propertyName,
        DiagnosticDescriptor rule)
    {
        // Find the named argument for the property.
        KeyValuePair<string, TypedConstant> namedArgument = attribute.NamedArguments
            .FirstOrDefault(kvp => kvp.Key == propertyName);

        if (namedArgument.Key is null)
        {
            return;
        }

        TypedConstant typedConstant = namedArgument.Value;

        // The property value should be an array of types.
        if (typedConstant.Kind != TypedConstantKind.Array)
        {
            return;
        }

        // Get the Exception type symbol for comparison.
        INamedTypeSymbol? exceptionTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionTypeSymbol is null)
        {
            return;
        }

        // Validate each type in the array.
        foreach (TypedConstant arrayElement in typedConstant.Values)
        {
            if (arrayElement.Kind != TypedConstantKind.Type ||
                arrayElement.Value is not INamedTypeSymbol typeSymbol)
            {
                continue;
            }

            // Check if the type inherits from Exception.
            if (!InheritsFromException(typeSymbol, exceptionTypeSymbol))
            {
                // Try to get precise location from syntax.
                Location location = GetExceptionTypeLocation(attribute, propertyName, typeSymbol) ??
                                    attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation() ??
                                    context.Symbol.Locations[0];

                var diagnostic = Diagnostic.Create(
                    rule,
                    location,
                    typeSymbol.ToDisplayString());

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    /// <summary>
    ///     Checks if a type inherits from System.Exception.
    /// </summary>
    /// <param name="typeSymbol">The type to check.</param>
    /// <param name="exceptionTypeSymbol">The System.Exception type symbol.</param>
    /// <returns>True if the type inherits from Exception, false otherwise.</returns>
    private static bool InheritsFromException(INamedTypeSymbol typeSymbol, INamedTypeSymbol exceptionTypeSymbol)
    {
        INamedTypeSymbol? currentType = typeSymbol;
        while (currentType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType, exceptionTypeSymbol))
            {
                return true;
            }

            currentType = currentType.BaseType;
        }

        return false;
    }

    /// <summary>
    ///     Attempts to get the specific location of an exception type in the attribute syntax.
    /// </summary>
    /// <param name="attribute">The attribute data.</param>
    /// <param name="propertyName">The property name.</param>
    /// <param name="typeSymbol">The type symbol to locate.</param>
    /// <returns>The location if found, null otherwise.</returns>
    private static Location? GetExceptionTypeLocation(
        AttributeData attribute,
        string propertyName,
        INamedTypeSymbol typeSymbol)
    {
        SyntaxNode? attributeSyntax = attribute.ApplicationSyntaxReference?.GetSyntax();
        if (attributeSyntax is not AttributeSyntax attrSyntax || attrSyntax.ArgumentList is null)
        {
            return null;
        }

        // Look for the named argument in the attribute syntax.
        foreach (AttributeArgumentSyntax argument in attrSyntax.ArgumentList.Arguments)
        {
            if (argument.NameEquals?.Name.Identifier.Text != propertyName)
            {
                continue;
            }

            // Find the specific typeof expression for this type.
            if (argument.Expression is not ImplicitArrayCreationExpressionSyntax &&
                argument.Expression is not ArrayCreationExpressionSyntax &&
                argument.Expression is not InitializerExpressionSyntax)
            {
                continue;
            }

            // Search for typeof expressions in the initializer.
            IEnumerable<TypeOfExpressionSyntax> typeOfExpressions = argument.Expression
                .DescendantNodes()
                .OfType<TypeOfExpressionSyntax>();

            foreach (TypeOfExpressionSyntax? typeOfExpr in typeOfExpressions)
            {
                var typeName = typeOfExpr.Type.ToString();
                if (typeName == typeSymbol.Name || typeName == typeSymbol.ToDisplayString())
                {
                    return typeOfExpr.GetLocation();
                }
            }
        }

        return null;
    }
}