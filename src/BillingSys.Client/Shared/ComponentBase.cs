using Microsoft.AspNetCore.Components;

namespace BillingSys.Client.Shared;

/// <summary>
/// Base class for Blazor components that provides common lifecycle patterns.
/// Implements IDisposable for proper cleanup of subscriptions and resources.
/// </summary>
public abstract class AppComponentBase : ComponentBase, IDisposable
{
    #region State Fields

    /// <summary>
    /// Indicates if the component is currently loading data
    /// </summary>
    protected bool _isLoading = true;

    /// <summary>
    /// Indicates if a save operation is in progress
    /// </summary>
    protected bool _isSaving;

    /// <summary>
    /// Current error message to display, null if no error
    /// </summary>
    protected string? _errorMessage;

    /// <summary>
    /// Current success message to display, null if no success message
    /// </summary>
    protected string? _successMessage;

    /// <summary>
    /// Tracks if the component has been disposed
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Cancellation token source for async operations
    /// </summary>
    protected CancellationTokenSource? _cts;

    #endregion

    #region Lifecycle

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _cts = new CancellationTokenSource();
        OnActivated();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        _isLoading = true;
        
        try
        {
            await OnActivatedAsync();
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Called when the component is activated. Override to perform initialization.
    /// </summary>
    protected virtual void OnActivated() { }

    /// <summary>
    /// Called when the component is activated. Override to perform async initialization.
    /// </summary>
    protected virtual Task OnActivatedAsync() => Task.CompletedTask;

    /// <summary>
    /// Called when the component is being disposed. Override to clean up resources.
    /// </summary>
    protected virtual void OnDeactivated() { }

    public void Dispose()
    {
        if (_disposed) return;
        
        OnDeactivated();
        
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        
        _disposed = true;
        
        GC.SuppressFinalize(this);
    }

    #endregion

    #region UI State Methods

    /// <summary>
    /// Sets the loading state and triggers a re-render
    /// </summary>
    protected void SetLoading(bool isLoading)
    {
        _isLoading = isLoading;
        StateHasChanged();
    }

    /// <summary>
    /// Shows an error message
    /// </summary>
    protected void ShowError(string message)
    {
        _errorMessage = message;
        _successMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Shows a success message
    /// </summary>
    protected void ShowSuccess(string message)
    {
        _successMessage = message;
        _errorMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Clears any messages
    /// </summary>
    protected void ClearMessages()
    {
        _errorMessage = null;
        _successMessage = null;
    }

    /// <summary>
    /// Wraps an async operation with loading state and error handling
    /// </summary>
    protected async Task ExecuteWithLoadingAsync(Func<Task> operation, string errorPrefix = "Operation failed")
    {
        if (_disposed) return;
        
        _isLoading = true;
        ClearMessages();
        StateHasChanged();
        
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation
        }
        catch (Exception ex)
        {
            ShowError($"{errorPrefix}: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Wraps a save operation with state and error handling
    /// </summary>
    protected async Task<bool> ExecuteSaveAsync(Func<Task<bool>> operation, string errorPrefix = "Save failed")
    {
        if (_disposed) return false;
        
        _isSaving = true;
        ClearMessages();
        StateHasChanged();
        
        try
        {
            var result = await operation();
            if (result)
            {
                ShowSuccess("Saved successfully");
            }
            return result;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            ShowError($"{errorPrefix}: {ex.Message}");
            return false;
        }
        finally
        {
            _isSaving = false;
            StateHasChanged();
        }
    }

    #endregion
}
