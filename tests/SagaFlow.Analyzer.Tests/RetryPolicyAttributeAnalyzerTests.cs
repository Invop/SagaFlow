using Microsoft.CodeAnalysis.Testing;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<SagaFlow.Analyzer.RetryPolicyAttributeAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace SagaFlow.Analyzer.Tests;

/// <summary>
///     Tests for RetryPolicyAttributeAnalyzer to ensure proper validation of RetryPolicyAttribute usage.
/// </summary>
public class RetryPolicyAttributeAnalyzerTests
{
    private const string RetryPolicyBaseCode = """
                                               namespace SagaFlow.Attributes
                                               {
                                                   using System;

                                                   [AttributeUsage(AttributeTargets.Class)]
                                                   public sealed class RetryPolicyAttribute : Attribute
                                                   {
                                                       public int MaxRetries { get; set; } = 3;
                                                       public int RetryDelayMilliseconds { get; set; } = 1000;
                                                       public BackoffStrategy Strategy { get; set; } = BackoffStrategy.Exponential;
                                                       public bool UseJitter { get; set; } = true;
                                                       public Type[]? RetryOnExceptions { get; set; }
                                                       public Type[]? NonRetryableExceptions { get; set; }
                                                       public int TimeoutMilliseconds { get; set; }
                                                       public int MaxRetryDelayMilliseconds { get; set; } = 30000;
                                                       public bool EnableCircuitBreaker { get; set; }
                                                       public int CircuitBreakerThreshold { get; set; } = 5;
                                                       public int CircuitBreakerDurationSeconds { get; set; } = 60;
                                                   }

                                                   public enum BackoffStrategy
                                                   {
                                                       Constant,
                                                       Linear,
                                                       Exponential
                                                   }
                                               }

                                               namespace SagaFlow.Messages
                                               {
                                                   using System;
                                                   using System.Threading;
                                                   using System.Threading.Tasks;

                                                   public interface ISagaMessage
                                                   {
                                                       string IdempotencyKey { get; }
                                                       Guid CorrelationId { get; }
                                                       DateTimeOffset Timestamp { get; }
                                                   }

                                                   public interface ISagaCommand : ISagaMessage { }

                                                   public interface ISagaEvent : ISagaMessage
                                                   {
                                                       string StepId { get; }
                                                       bool IsSuccess { get; }
                                                       string? ErrorMessage { get; }
                                                   }

                                                   public interface ISagaMessageContext<out TMessage> where TMessage : ISagaMessage
                                                   {
                                                       TMessage Message { get; }
                                                       Guid CorrelationId { get; }
                                                       string MessageId { get; }
                                                       string SenderId { get; }
                                                   }

                                                   public interface ISagaCommandHandler<in TCommand> where TCommand : ISagaCommand
                                                   {
                                                       ValueTask HandleAsync(ISagaMessageContext<TCommand> context, CancellationToken cancellationToken = default);
                                                   }

                                                   public interface ISagaEventHandler<in TEvent, in TState> where TEvent : ISagaEvent
                                                   {
                                                       ValueTask HandleAsync(ISagaMessageContext<TEvent> context, TState state, CancellationToken cancellationToken = default);
                                                   }
                                               }
                                               """;

    #region Handler Implementation Tests (CHSG0003)

    [Fact]
    public async Task RetryPolicyOnCommandHandler_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(MaxRetries = 3)]
                                                      public class ProcessOrderCommandHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                          {
                                                              return default;
                                                          }
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RetryPolicyOnEventHandler_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record OrderProcessedEvent : ISagaEvent
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                          public string StepId => "step1";
                                                          public bool IsSuccess => true;
                                                          public string? ErrorMessage => null;
                                                      }

                                                      public class OrderState { }

                                                      [RetryPolicy(MaxRetries = 5)]
                                                      public class OrderProcessedEventHandler : ISagaEventHandler<OrderProcessedEvent, OrderState>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<OrderProcessedEvent> context, OrderState state, CancellationToken cancellationToken = default)
                                                          {
                                                              return default;
                                                          }
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RetryPolicyOnNonHandlerClass_ReportsDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using SagaFlow.Attributes;

                                                      [RetryPolicy(MaxRetries = 3)]
                                                      public class OrderService
                                                      {
                                                          public void Process()
                                                          {
                                                          }
                                                      }
                                                  }
                                                  """;

        DiagnosticResult expected = Verify.Diagnostic(RetryPolicyAttributeAnalyzer.HandlerRequiredRuleDiagnosticId)
            .WithLocation(73, 6)
            .WithArguments("OrderService");

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task RetryPolicyOnCommand_ReportsDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      [RetryPolicy(MaxRetries = 3)]
                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }
                                                  }
                                                  """;

        DiagnosticResult expected = Verify.Diagnostic(RetryPolicyAttributeAnalyzer.HandlerRequiredRuleDiagnosticId)
            .WithLocation(75, 6)
            .WithArguments("ProcessOrderCommand");

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task RetryPolicyOnDerivedHandler_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      public abstract class BaseCommandHandler<TCommand> : ISagaCommandHandler<TCommand> where TCommand : ISagaCommand
                                                      {
                                                          public abstract ValueTask HandleAsync(ISagaMessageContext<TCommand> context, CancellationToken cancellationToken = default);
                                                      }

                                                      [RetryPolicy(MaxRetries = 3)]
                                                      public class ProcessOrderCommandHandler : BaseCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public override ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                          {
                                                              return default;
                                                          }
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    #endregion

    #region Exception Types Tests (CHSG0001, CHSG0002)

    [Fact]
    public async Task ValidRetryPolicyAttributeWithStandardExceptions_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(RetryOnExceptions = new[] { typeof(InvalidOperationException), typeof(TimeoutException) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidRetryPolicyAttributeWithNonRetryableExceptions_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(NonRetryableExceptions = new[] { typeof(ArgumentException), typeof(ArgumentNullException) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidRetryPolicyAttributeWithBothExceptionTypes_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(
                                                          RetryOnExceptions = new[] { typeof(InvalidOperationException) },
                                                          NonRetryableExceptions = new[] { typeof(ArgumentException) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidRetryPolicyAttributeWithCustomException_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public class OrderProcessingException : Exception
                                                      {
                                                      }

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(RetryOnExceptions = new[] { typeof(OrderProcessingException) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidRetryPolicyAttributeWithInheritedCustomException_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public class OrderProcessingException : Exception { }

                                                      public class OrderTimeoutException : OrderProcessingException { }

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(RetryOnExceptions = new[] { typeof(OrderTimeoutException) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RetryOnExceptionsWithNonExceptionType_ReportsDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public class InvalidExceptionType { }

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(RetryOnExceptions = new[] { typeof(InvalidExceptionType) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        DiagnosticResult expected = Verify.Diagnostic(RetryPolicyAttributeAnalyzer.RetryOnExceptionsRuleDiagnosticId)
            .WithLocation(86, 46)
            .WithArguments("TestNamespace.InvalidExceptionType");

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task NonRetryableExceptionsWithNonExceptionType_ReportsDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public class InvalidExceptionType { }

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(NonRetryableExceptions = new[] { typeof(InvalidExceptionType) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        DiagnosticResult expected = Verify.Diagnostic(RetryPolicyAttributeAnalyzer.NonRetryableExceptionsRuleDiagnosticId)
            .WithLocation(86, 51)
            .WithArguments("TestNamespace.InvalidExceptionType");

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task BothExceptionPropertiesWithInvalidTypes_ReportsBothDiagnostics()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public class InvalidRetryType { }
                                                      public class InvalidNonRetryType { }

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(
                                                          RetryOnExceptions = new[] { typeof(InvalidRetryType) },
                                                          NonRetryableExceptions = new[] { typeof(InvalidNonRetryType) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        DiagnosticResult[] expected =
        [
            Verify.Diagnostic(RetryPolicyAttributeAnalyzer.RetryOnExceptionsRuleDiagnosticId)
                .WithLocation(88, 37)
                .WithArguments("TestNamespace.InvalidRetryType"),
            Verify.Diagnostic(RetryPolicyAttributeAnalyzer.NonRetryableExceptionsRuleDiagnosticId)
                .WithLocation(89, 42)
                .WithArguments("TestNamespace.InvalidNonRetryType")
        ];

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task RetryOnExceptionsWithMultipleInvalidTypes_ReportsMultipleDiagnostics()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public class InvalidType1 { }
                                                      public class InvalidType2 { }

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(RetryOnExceptions = new[] { typeof(InvalidType1), typeof(InvalidType2) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        DiagnosticResult[] expected =
        [
            Verify.Diagnostic(RetryPolicyAttributeAnalyzer.RetryOnExceptionsRuleDiagnosticId)
                .WithLocation(87, 46)
                .WithArguments("TestNamespace.InvalidType1"),
            Verify.Diagnostic(RetryPolicyAttributeAnalyzer.RetryOnExceptionsRuleDiagnosticId)
                .WithLocation(87, 68)
                .WithArguments("TestNamespace.InvalidType2")
        ];

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task RetryOnExceptionsWithMixedValidAndInvalidTypes_ReportsDiagnosticForInvalidOnly()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public class InvalidType { }

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(RetryOnExceptions = new[] { typeof(InvalidOperationException), typeof(InvalidType) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        DiagnosticResult expected = Verify.Diagnostic(RetryPolicyAttributeAnalyzer.RetryOnExceptionsRuleDiagnosticId)
            .WithLocation(86, 81)
            .WithArguments("TestNamespace.InvalidType");

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    [Fact]
    public async Task ValidRetryPolicyAttributeWithNoExceptionProperties_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(MaxRetries = 5, RetryDelayMilliseconds = 2000)]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidRetryPolicyAttributeWithAllParameters_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(
                                                          MaxRetries = 5,
                                                          RetryDelayMilliseconds = 2000,
                                                          Strategy = BackoffStrategy.Linear,
                                                          UseJitter = false,
                                                          RetryOnExceptions = new[] { typeof(InvalidOperationException) },
                                                          NonRetryableExceptions = new[] { typeof(ArgumentException) },
                                                          TimeoutMilliseconds = 5000,
                                                          MaxRetryDelayMilliseconds = 10000,
                                                          EnableCircuitBreaker = true,
                                                          CircuitBreakerThreshold = 3,
                                                          CircuitBreakerDurationSeconds = 30)]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RetryPolicyAttributeOnInterface_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using SagaFlow.Attributes;

                                                      public interface IOrderSaga
                                                      {
                                                          void Process();
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task RetryPolicyAttributeOnStruct_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using SagaFlow.Attributes;

                                                      public struct OrderData
                                                      {
                                                          public string Id { get; set; }
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task ValidRetryPolicyAttributeWithSystemIOException_NoDiagnostic()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.IO;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(RetryOnExceptions = new[] { typeof(IOException), typeof(FileNotFoundException) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        await Verify.VerifyAnalyzerAsync(test);
    }

    [Fact]
    public async Task NonRetryableExceptionsWithMultipleInvalidTypes_ReportsMultipleDiagnostics()
    {
        const string test = RetryPolicyBaseCode + """

                                                  namespace TestNamespace
                                                  {
                                                      using System;
                                                      using System.Threading;
                                                      using System.Threading.Tasks;
                                                      using SagaFlow.Attributes;
                                                      using SagaFlow.Messages;

                                                      public class InvalidType1 { }
                                                      public class InvalidType2 { }

                                                      public record ProcessOrderCommand : ISagaCommand
                                                      {
                                                          public string IdempotencyKey => "key";
                                                          public Guid CorrelationId => Guid.NewGuid();
                                                          public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
                                                      }

                                                      [RetryPolicy(NonRetryableExceptions = new[] { typeof(InvalidType1), typeof(InvalidType2) })]
                                                      public class OrderHandler : ISagaCommandHandler<ProcessOrderCommand>
                                                      {
                                                          public ValueTask HandleAsync(ISagaMessageContext<ProcessOrderCommand> context, CancellationToken cancellationToken = default)
                                                              => default;
                                                      }
                                                  }
                                                  """;

        DiagnosticResult[] expected =
        [
            Verify.Diagnostic(RetryPolicyAttributeAnalyzer.NonRetryableExceptionsRuleDiagnosticId)
                .WithLocation(87, 51)
                .WithArguments("TestNamespace.InvalidType1"),
            Verify.Diagnostic(RetryPolicyAttributeAnalyzer.NonRetryableExceptionsRuleDiagnosticId)
                .WithLocation(87, 73)
                .WithArguments("TestNamespace.InvalidType2")
        ];

        await Verify.VerifyAnalyzerAsync(test, expected);
    }

    #endregion
}
