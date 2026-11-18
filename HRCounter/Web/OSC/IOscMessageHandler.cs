using System;
using System.Collections.Generic;

namespace HRCounter.Web.OSC;

/// <summary>
///     Handles OSC messages sent to its addresses. Register it with <see cref="IOscServer" />.
/// </summary>
public interface IOscMessageHandler
{
    /// <summary>
    ///     The OSC addresses to handle, e.g. <c>/avatar/parameters/HR</c>.
    ///     Only read on register and <see cref="IOscServer.UpdateHandler" />.
    /// </summary>
    IReadOnlyList<string> Addresses { get; }

    /// <summary>
    ///     Called on the OSC receive thread, so it must not block.
    ///     May still be called briefly after the handler is unregistered.
    /// </summary>
    /// <param name="arguments">The type tags, without the leading comma.</param>
    /// <param name="data">The argument data.</param>
    void HandleMessage(ReadOnlySpan<byte> arguments, ReadOnlySpan<byte> data);
}
