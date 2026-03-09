namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents the possible states of the OpenCode Server process.
/// </summary>
public enum OpenCodeServerState
{
    /// <summary>The server state has not been determined yet (initial state).</summary>
    Unknown,

    /// <summary>The server process is starting up.</summary>
    Starting,

    /// <summary>The server is running and responding to health checks.</summary>
    Running,

    /// <summary>The server process is being stopped.</summary>
    Stopping,

    /// <summary>The server process is not running.</summary>
    Stopped,

    /// <summary>The server process encountered an error.</summary>
    Error
}
