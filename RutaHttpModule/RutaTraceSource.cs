namespace RutaHttpModule
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Wrapper class for using <see cref="TraceSource"/>.
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal class RutaTraceSource : ITraceSource
    {
        /// <summary>
        /// The name of the trace source (typically, the name of the application).
        /// </summary>
        private readonly string tracesourceName;

        /// <summary>
        /// Internal <see cref="TraceSource"/> reference.
        /// </summary>
        private readonly TraceSource traceSource;

        /// <summary>
        /// Creates a <see cref="RutaTraceSource"/> based on the supplied
        /// <paramref name="tracesourceName"/>.
        /// </summary>
        /// <param name="tracesourceName">The name of the trace source.</param>
        internal RutaTraceSource(string tracesourceName)
        {
            this.traceSource = new TraceSource(tracesourceName);
            this.tracesourceName = tracesourceName;         
        }

        /// <summary>
        /// Writes a trace event message to the trace listeners using the specified event type, 
        /// event identifier, and message.
        /// </summary>
        /// <param name="eventType">One of the enumeration values that specifies the event type of the trace data.</param>
        /// <param name="id">An event identifier.</param>
        /// <param name="message">The trace message to write.</param>
        public void TraceEvent(TraceEventType eventType, int id, string message)
        {
            traceSource.TraceEvent(eventType, id, $"[{tracesourceName} MODULE] {message}");
        }
    }
}