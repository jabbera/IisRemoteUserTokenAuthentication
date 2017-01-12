namespace RutaHttpModule
{
    using System.Diagnostics;

    /// <summary>
    /// Interface for implementations sending trace messages.
    /// </summary>
    internal interface ITraceSource
    {
        /// <summary>
        /// Writes a trace event message to the trace listeners using the specified event type, 
        /// event identifier, and message.
        /// </summary>
        /// <param name="eventType">One of the enumeration values that specifies the event type of the trace data.</param>
        /// <param name="id">An event identifier.</param>
        /// <param name="message">The trace message to write.</param>
        void TraceEvent(TraceEventType eventType, int id, string message);
    }
}
