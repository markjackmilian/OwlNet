using DispatchR.Abstractions.Send;
using Microsoft.JSInterop;
using OwlNet.Application.Common.Models;
using OwlNet.Application.Projects.Queries.GetProjectById;

namespace OwlNet.Web.Services;

/// <summary>
/// Manages the currently active project context for the application.
/// Persists the active project ID in the browser's <c>sessionStorage</c> so it
/// survives page refreshes within the same browser tab.
/// Raises <see cref="OnActiveProjectChanged"/> when the active project changes
/// so that components can react (e.g., update the topbar indicator).
/// </summary>
public sealed class ActiveProjectService : IDisposable
{
    private const string SessionStorageKey = "owlnet_active_project_id";

    private readonly IJSRuntime _jsRuntime;
    private readonly IRequestHandler<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>> _getProjectHandler;

    /// <summary>
    /// Gets the currently active project, or <see langword="null"/> if no project is selected.
    /// </summary>
    public ProjectDto? ActiveProject { get; private set; }

    /// <summary>
    /// Event raised when the active project changes (selected, cleared, or updated).
    /// Subscribers should call <c>StateHasChanged()</c> to re-render.
    /// </summary>
    public event Action? OnActiveProjectChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActiveProjectService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript interop runtime for sessionStorage access.</param>
    /// <param name="getProjectHandler">The handler to verify project existence on restore.</param>
    public ActiveProjectService(
        IJSRuntime jsRuntime,
        IRequestHandler<GetProjectByIdQuery, ValueTask<Result<ProjectDto>>> getProjectHandler)
    {
        _jsRuntime = jsRuntime;
        _getProjectHandler = getProjectHandler;
    }

    /// <summary>
    /// Sets the specified project as the active project and persists its ID to sessionStorage.
    /// </summary>
    /// <param name="project">The project to set as active.</param>
    public async Task SetActiveProjectAsync(ProjectDto project)
    {
        ActiveProject = project;
        await SetSessionStorageAsync(project.Id.ToString());
        OnActiveProjectChanged?.Invoke();
    }

    /// <summary>
    /// Clears the active project context and removes the stored ID from sessionStorage.
    /// </summary>
    public async Task ClearActiveProjectAsync()
    {
        ActiveProject = null;
        await RemoveSessionStorageAsync();
        OnActiveProjectChanged?.Invoke();
    }

    /// <summary>
    /// Updates the active project data in memory (e.g., after an edit).
    /// Does not change the sessionStorage ID since the project ID is unchanged.
    /// </summary>
    /// <param name="updatedProject">The updated project data.</param>
    public void UpdateActiveProjectData(ProjectDto updatedProject)
    {
        if (ActiveProject is not null && ActiveProject.Id == updatedProject.Id)
        {
            ActiveProject = updatedProject;
            OnActiveProjectChanged?.Invoke();
        }
    }

    /// <summary>
    /// Attempts to restore the active project from sessionStorage on application load.
    /// Verifies the project still exists and is not archived. If invalid, clears the stored ID.
    /// </summary>
    public async Task RestoreFromSessionStorageAsync()
    {
        var storedId = await GetSessionStorageAsync();

        if (string.IsNullOrEmpty(storedId) || !Guid.TryParse(storedId, out var projectId))
        {
            return;
        }

        var query = new GetProjectByIdQuery { Id = projectId };
        var result = await _getProjectHandler.Handle(query, CancellationToken.None);

        if (result.IsSuccess && !result.Value.IsArchived)
        {
            ActiveProject = result.Value;
            OnActiveProjectChanged?.Invoke();
        }
        else
        {
            await RemoveSessionStorageAsync();
        }
    }

    /// <summary>
    /// Gets the stored project ID from sessionStorage.
    /// </summary>
    private async Task<string?> GetSessionStorageAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>(
                "sessionStorage.getItem",
                SessionStorageKey);
        }
        catch (InvalidOperationException)
        {
            // JS interop not available during prerendering
            return null;
        }
    }

    /// <summary>
    /// Sets the project ID in sessionStorage.
    /// </summary>
    private async Task SetSessionStorageAsync(string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "sessionStorage.setItem",
                SessionStorageKey,
                value);
        }
        catch (InvalidOperationException)
        {
            // JS interop not available during prerendering
        }
    }

    /// <summary>
    /// Removes the project ID from sessionStorage.
    /// </summary>
    private async Task RemoveSessionStorageAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync(
                "sessionStorage.removeItem",
                SessionStorageKey);
        }
        catch (InvalidOperationException)
        {
            // JS interop not available during prerendering
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged resources to clean up.
        // Event subscribers are responsible for unsubscribing.
    }
}
