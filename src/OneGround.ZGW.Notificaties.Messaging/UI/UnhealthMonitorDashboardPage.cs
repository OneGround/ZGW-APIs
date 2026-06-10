using System.Text;
using System.Text.Encodings.Web;
using Hangfire.Dashboard;
using OneGround.ZGW.Notificaties.Messaging.CircuitBreaker.Services;

namespace OneGround.ZGW.Notificaties.Messaging.UI;

public class UnhealthMonitorDashboardPage : IDashboardDispatcher
{
    private readonly ICircuitBreakerSubscriberHealthTracker _healthTracker;

    public UnhealthMonitorDashboardPage(ICircuitBreakerSubscriberHealthTracker healthTracker)
    {
        _healthTracker = healthTracker;
    }

    public async Task Dispatch(DashboardContext context)
    {
        // POST clears the cache (all entries, or only the supplied "key" values); GET renders the page.
        if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            await HandleClearRequestAsync(context);
            return;
        }

        var states = await _healthTracker.GetAllUnhealthyAsync();

        // Note: This HTML is fully Co-pilot generated.
        var response = context.Response;
        response.ContentType = "text/html";

        var tableRows = new StringBuilder();
        foreach (var (key, state) in states)
        {
            var statusClass = state.IsCircuitOpen ? "status-blocked" : "status-warning";
            var statusText = state.IsCircuitOpen ? "BLOCKED" : "MONITORING";
            var encodedKey = System.Net.WebUtility.HtmlEncode(key.ToString());
            var encodedUrl = System.Net.WebUtility.HtmlEncode(state.Url);

            tableRows.AppendLine(
                $@"
                <tr class='{statusClass}'>
                    <td>{encodedUrl}</td>
                    <td class='status-badge'>{statusText}</td>
                    <td>{state.ConsecutiveFailures}</td>
                    <td>{FormatDateTime(state.FirstFailureAt)}</td>
                    <td>{FormatDateTime(state.LastFailureAt)}</td>
                    <td>{FormatDateTime(state.BlockedUntil)}</td>
                    <td>{state.LastStatusCode?.ToString() ?? "N/A"}</td>
                    <td class='error-message'>{System.Net.WebUtility.HtmlEncode(state.LastErrorMessage ?? "N/A")}</td>
                    <td class='actions-cell'>
                        <button class='delete-btn' data-key='{encodedKey}' data-url='{encodedUrl}' onclick='deleteItem(this)'>Delete</button>
                    </td>
                </tr>"
            );
        }

        var htmlContent =
            $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Unhealthy Web-hook Receivers</title>
                <style>
                    body {{
                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        margin: 0;
                        padding: 20px;
                        background-color: #f5f5f5;
                    }}
                    .container {{
                        max-width: 1400px;
                        margin: 0 auto;
                        background: white;
                        padding: 30px;
                        border-radius: 8px;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    }}
                    .header {{
                        display: flex;
                        justify-content: space-between;
                        align-items: center;
                        margin-bottom: 10px;
                    }}
                    h1 {{
                        color: #333;
                        margin: 0;
                    }}
                    .header-actions {{
                        display: flex;
                        gap: 15px;
                        align-items: center;
                    }}
                    .back-link {{
                        color: #000000;
                        text-decoration: none;
                        font-weight: 500;
                        cursor: pointer;
                    }}
                    .back-link:hover {{
                        text-decoration: underline;
                    }}
                    .clear-cache-btn {{
                        background-color: #dc3545;
                        color: white;
                        border: none;
                        padding: 10px 20px;
                        border-radius: 5px;
                        font-weight: 500;
                        cursor: pointer;
                        font-size: 14px;
                        transition: background-color 0.2s;
                    }}
                    .clear-cache-btn:hover {{
                        background-color: #c82333;
                    }}
                    .clear-cache-btn:disabled {{
                        background-color: #6c757d;
                        cursor: not-allowed;
                    }}
                    .toggle-container {{
                        display: flex;
                        align-items: center;
                        gap: 10px;
                    }}
                    .toggle-switch {{
                        position: relative;
                        display: inline-block;
                        width: 50px;
                        height: 24px;
                    }}
                    .toggle-switch input {{
                        opacity: 0;
                        width: 0;
                        height: 0;
                    }}
                    .toggle-slider {{
                        position: absolute;
                        cursor: pointer;
                        top: 0;
                        left: 0;
                        right: 0;
                        bottom: 0;
                        background-color: #ccc;
                        transition: .4s;
                        border-radius: 24px;
                    }}
                    .toggle-slider:before {{
                        position: absolute;
                        content: "";
                        height: 18px;
                        width: 18px;
                        left: 3px;
                        bottom: 3px;
                        background-color: white;
                        transition: .4s;
                        border-radius: 50%;
                    }}
                    input:checked + .toggle-slider {{
                        background-color: #28a745;
                    }}
                    input:checked + .toggle-slider:before {{
                        transform: translateX(26px);
                    }}
                    .toggle-label {{
                        font-size: 14px;
                        font-weight: 500;
                        color: #333;
                        user-select: none;
                    }}
                    .summary {{
                        background: #f8f9fa;
                        padding: 15px;
                        border-radius: 5px;
                        margin-bottom: 20px;
                        border-left: 4px solid #007bff;
                    }}
                    table {{
                        width: 100%;
                        border-collapse: collapse;
                        margin-top: 20px;
                    }}
                    th {{
                        background-color: #343a40;
                        color: white;
                        padding: 12px;
                        text-align: left;
                        font-weight: 600;
                    }}
                    td {{
                        padding: 12px;
                        border-bottom: 1px solid #dee2e6;
                    }}
                    tr:hover {{
                        background-color: #f8f9fa;
                    }}
                    .status-blocked {{
                        background-color: #ffe5e5;
                    }}
                    .status-warning {{
                        background-color: #fff8e1;
                    }}
                    .status-badge {{
                        font-weight: bold;
                        font-size: 14px;
                    }}
                    .error-message {{
                        max-width: 300px;
                        word-wrap: break-word;
                        font-size: 12px;
                        color: #666;
                    }}
                    .actions-cell {{
                        white-space: nowrap;
                    }}
                    .delete-btn {{
                        background-color: #dc3545;
                        color: white;
                        border: none;
                        padding: 6px 14px;
                        border-radius: 4px;
                        font-weight: 500;
                        cursor: pointer;
                        font-size: 13px;
                        transition: background-color 0.2s;
                    }}
                    .delete-btn:hover {{
                        background-color: #c82333;
                    }}
                    .delete-btn:disabled {{
                        background-color: #6c757d;
                        cursor: not-allowed;
                    }}
                    .empty-state {{
                        text-align: center;
                        padding: 40px;
                        color: #6c757d;
                    }}
                    .empty-state-icon {{
                        font-size: 48px;
                        margin-bottom: 10px;
                    }}
                    .timestamp {{
                        font-size: 12px;
                        color: #6c757d;
                    }}
                    .notification {{
                        position: fixed;
                        top: 20px;
                        right: 20px;
                        background-color: #28a745;
                        color: white;
                        padding: 15px 20px;
                        border-radius: 5px;
                        box-shadow: 0 4px 6px rgba(0,0,0,0.2);
                        display: none;
                        z-index: 1000;
                    }}
                    .notification.show {{
                        display: block;
                        animation: slideIn 0.3s ease-out;
                    }}
                    @keyframes slideIn {{
                        from {{
                            transform: translateX(400px);
                            opacity: 0;
                        }}
                        to {{
                            transform: translateX(0);
                            opacity: 1;
                        }}
                    }}
                    .modal-overlay {{
                        position: fixed;
                        top: 0;
                        left: 0;
                        right: 0;
                        bottom: 0;
                        background-color: rgba(0,0,0,0.5);
                        display: none;
                        align-items: center;
                        justify-content: center;
                        z-index: 2000;
                    }}
                    .modal-overlay.show {{
                        display: flex;
                    }}
                    .modal {{
                        background: white;
                        padding: 25px 30px;
                        border-radius: 8px;
                        box-shadow: 0 6px 20px rgba(0,0,0,0.3);
                        max-width: 480px;
                        width: 90%;
                    }}
                    .modal h2 {{
                        margin: 0 0 12px 0;
                        font-size: 18px;
                        color: #333;
                    }}
                    .modal p {{
                        margin: 0 0 20px 0;
                        color: #555;
                        font-size: 14px;
                        word-break: break-word;
                    }}
                    .modal-buttons {{
                        display: flex;
                        justify-content: flex-end;
                        gap: 10px;
                    }}
                    .modal-btn {{
                        border: none;
                        padding: 10px 18px;
                        border-radius: 5px;
                        font-weight: 500;
                        cursor: pointer;
                        font-size: 14px;
                        transition: background-color 0.2s;
                    }}
                    .modal-btn-confirm {{
                        background-color: #dc3545;
                        color: white;
                    }}
                    .modal-btn-confirm:hover {{
                        background-color: #c82333;
                    }}
                    .modal-btn-cancel {{
                        background-color: #e9ecef;
                        color: #333;
                    }}
                    .modal-btn-cancel:hover {{
                        background-color: #d3d9df;
                    }}
                </style>
                <script>
                    let refreshIntervalId = null;
                    let pendingAction = null;

                    function refreshTable() {{
                        fetch(window.location.href)
                            .then(response => response.text())
                            .then(html => {{
                                const parser = new DOMParser();
                                const doc = parser.parseFromString(html, 'text/html');

                                const newSummary = doc.querySelector('.summary');
                                const currentSummary = document.querySelector('.summary');
                                if (newSummary && currentSummary) {{
                                    currentSummary.innerHTML = newSummary.innerHTML;
                                }}

                                const newContent = doc.querySelector('#table-content');
                                const currentContent = document.querySelector('#table-content');
                                if (newContent && currentContent) {{
                                    currentContent.innerHTML = newContent.innerHTML;
                                }}

                                const newTimestamp = doc.querySelector('.timestamp');
                                const currentTimestamp = document.querySelector('.timestamp');
                                if (newTimestamp && currentTimestamp) {{
                                    currentTimestamp.innerHTML = newTimestamp.innerHTML;
                                }}
                            }})
                            .catch(error => console.error('Error refreshing table:', error));
                    }}

                    function toggleAutoRefresh() {{
                        const toggle = document.getElementById('autoRefreshToggle');
                        const isEnabled = toggle.checked;

                        if (isEnabled) {{
                            // Start auto-refresh every 3 seconds
                            refreshIntervalId = setInterval(refreshTable, 3000);
                            localStorage.setItem('autoRefreshEnabled', 'true');
                        }} else {{
                            // Stop auto-refresh
                            if (refreshIntervalId) {{
                                clearInterval(refreshIntervalId);
                                refreshIntervalId = null;
                            }}
                            localStorage.setItem('autoRefreshEnabled', 'false');
                        }}
                    }}

                    function initializeAutoRefresh() {{
                        const toggle = document.getElementById('autoRefreshToggle');
                        const savedState = localStorage.getItem('autoRefreshEnabled');

                        // Default to enabled if not set
                        if (savedState === null || savedState === 'true') {{
                            toggle.checked = true;
                            refreshIntervalId = setInterval(refreshTable, 3000);
                        }} else {{
                            toggle.checked = false;
                        }}
                    }}

                    // Confirmation modal helpers
                    function openConfirmModal(title, message, confirmLabel, action) {{
                        pendingAction = action;
                        document.getElementById('modalTitle').textContent = title;
                        document.getElementById('modalMessage').textContent = message;
                        const confirmBtn = document.getElementById('modalConfirmBtn');
                        confirmBtn.textContent = confirmLabel;
                        confirmBtn.disabled = false;
                        document.getElementById('confirmModal').classList.add('show');
                        // Move keyboard focus into the dialog for accessibility
                        confirmBtn.focus();
                    }}

                    function closeConfirmModal() {{
                        pendingAction = null;
                        document.getElementById('modalConfirmBtn').disabled = false;
                        document.getElementById('confirmModal').classList.remove('show');
                    }}

                    function confirmModalProceed() {{
                        const action = pendingAction;
                        // Guard against rapid double-clicks: ignore if there is no pending action
                        if (typeof action !== 'function') {{
                            return;
                        }}
                        pendingAction = null;
                        document.getElementById('modalConfirmBtn').disabled = true;
                        document.getElementById('confirmModal').classList.remove('show');
                        action();
                    }}

                    // Ask for confirmation before clearing the whole cache
                    function clearCache() {{
                        openConfirmModal(
                            'Clear all unhealthy subscribers',
                            'Are you sure you want to clear the cache for ALL unhealthy subscribers? This cannot be undone.',
                            'Clear all',
                            submitClearAllRequest
                        );
                    }}

                    // Shared POST helper - returns the parsed JSON response
                    async function postClearRequest(body) {{
                        const response = await fetch(window.location.href, {{
                            method: 'POST',
                            headers: {{
                                'Content-Type': 'application/x-www-form-urlencoded'
                            }},
                            body: body
                        }});
                        return response.json();
                    }}

                    async function submitClearAllRequest() {{
                        const btn = document.getElementById('clearCacheBtn');
                        btn.disabled = true;
                        btn.textContent = 'Clearing...';

                        try {{
                            const data = await postClearRequest('');
                            if (data.success) {{
                                showNotification(data.message || 'Cache cleared successfully');
                                setTimeout(refreshTable, 500);
                            }} else {{
                                showNotification(data.message || 'Failed to clear cache', true);
                            }}
                        }} catch (error) {{
                            console.error('Error clearing cache:', error);
                            showNotification('Error clearing cache: ' + error.message, true);
                        }} finally {{
                            btn.disabled = false;
                            btn.textContent = 'Clear all';
                        }}
                    }}

                    // Ask for confirmation before deleting a single subscriber
                    function deleteItem(button) {{
                        const key = button.getAttribute('data-key');
                        const url = button.getAttribute('data-url') || 'this subscriber';
                        openConfirmModal(
                            'Delete subscriber',
                            'Are you sure you want to remove ' + url + ' from the unhealthy cache?',
                            'Delete',
                            () => submitDeleteRequest(key, button)
                        );
                    }}

                    function resetDeleteButton(button) {{
                        if (button) {{
                            button.disabled = false;
                            button.textContent = 'Delete';
                        }}
                    }}

                    async function submitDeleteRequest(key, button) {{
                        if (button) {{
                            button.disabled = true;
                            button.textContent = 'Deleting...';
                        }}

                        try {{
                            const data = await postClearRequest('key=' + encodeURIComponent(key));
                            if (data.success) {{
                                showNotification(data.message || 'Subscriber removed successfully');
                                // On success the row disappears after the refresh, so no button reset needed
                                setTimeout(refreshTable, 500);
                            }} else {{
                                showNotification(data.message || 'Failed to remove subscriber', true);
                                resetDeleteButton(button);
                            }}
                        }} catch (error) {{
                            console.error('Error removing subscriber:', error);
                            showNotification('Error removing subscriber: ' + error.message, true);
                            resetDeleteButton(button);
                        }}
                    }}

                    function showNotification(message, isError = false) {{
                        const notification = document.getElementById('notification');
                        notification.textContent = message;
                        notification.style.backgroundColor = isError ? '#dc3545' : '#28a745';
                        notification.classList.add('show');

                        setTimeout(() => {{
                            notification.classList.remove('show');
                        }}, 3000);
                    }}

                    // Initialize auto-refresh on page load
                    window.addEventListener('DOMContentLoaded', initializeAutoRefresh);
                </script>
            </head>
            <body>
                <div id='notification' class='notification'></div>
                <div id='confirmModal' class='modal-overlay'>
                    <div class='modal' role='dialog' aria-modal='true' aria-labelledby='modalTitle' aria-describedby='modalMessage'>
                        <h2 id='modalTitle'>Please confirm</h2>
                        <p id='modalMessage'></p>
                        <div class='modal-buttons'>
                            <button class='modal-btn modal-btn-cancel' onclick='closeConfirmModal()'>Cancel</button>
                            <button id='modalConfirmBtn' class='modal-btn modal-btn-confirm' onclick='confirmModalProceed()'>Confirm</button>
                        </div>
                    </div>
                </div>
                <div class='container'>
                    <div class='header'>
                        <h1>Unhealthy notification receiver endpoints</h1>
                        <div class='header-actions'>
                            <div class='toggle-container'>
                                <span class='toggle-label'>Auto Refresh</span>
                                <label class='toggle-switch'>
                                    <input type='checkbox' id='autoRefreshToggle' onchange='toggleAutoRefresh()'>
                                    <span class='toggle-slider'></span>
                                </label>
                            </div>
                            <button id='clearCacheBtn' class='clear-cache-btn' onclick='clearCache()'>Clear all</button>
                            <a href='javascript:history.back()' class='back-link'>Back to Previous Page</a>
                        </div>
                    </div>
                    <div class='summary'>
                        <strong>Total Tracked:</strong> {states.Count} subscriber(s) |
                        <strong>Blocked:</strong> {states.Count(s => s.Value.IsCircuitOpen)} |
                        <strong>Monitoring:</strong> {states.Count(s => !s.Value.IsCircuitOpen)}
                    </div>

                    <div id='table-content'>
                        {(states.Count > 0 ? $@"
                        <table>
                            <thead>
                                <tr>
                                    <th>Subscriber URL</th>
                                    <th>Status</th>
                                    <th>Failures</th>
                                    <th>First Failure</th>
                                    <th>Last Failure</th>
                                    <th>Blocked Until</th>
                                    <th>Status Code</th>
                                    <th>Last Error</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {tableRows}
                            </tbody>
                        </table>" : @"
                        <div class='empty-state'>
                            <h3>All Services Healthy</h3>
                            <p>No unhealthy webhook receivers detected.</p>
                        </div>")}
                    </div>

                    <div class='timestamp'>
                        Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                    </div>
                </div>
            </body>
            </html>
        ";

        await response.WriteAsync(htmlContent);
    }

    private async Task HandleClearRequestAsync(DashboardContext context)
    {
        try
        {
            var keyValues = await context.Request.GetFormValuesAsync("key");
            var keys = keyValues?.Where(k => !string.IsNullOrWhiteSpace(k)).ToArray() ?? [];

            var (clearedCount, message) = await ClearSubscribersAsync(keys);

            // Build the JSON payload first, then keep the response I/O on its own.
            var payload = $"{{\"success\": true, \"cleared\": {clearedCount}, \"message\": \"{JavaScriptEncoder.Default.Encode(message)}\"}}";

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(payload);
        }
        catch (OperationCanceledException)
        {
            // Request aborted (e.g. client disconnected): let cancellation propagate instead of
            // masking it as a 500 response.
            throw;
        }
        catch (Exception ex)
        {
            // Deliberate error boundary for this dashboard endpoint. The most likely failure is the
            // Redis-backed cache being unavailable (a StackExchange.Redis exception), which we want to
            // surface to the page as a readable JSON error rather than an unhandled 500.
            var errorPayload = $"{{\"success\": false, \"message\": \"Error: {JavaScriptEncoder.Default.Encode(ex.Message)}\"}}";

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(errorPayload);
        }
    }

    private async Task<(int ClearedCount, string Message)> ClearSubscribersAsync(string[] keys)
    {
        // No keys means "clear everything".
        if (keys.Length == 0)
        {
            var clearedAll = await _healthTracker.ClearAllUnhealthyAsync(CancellationToken.None);
            return (clearedAll, $"Cleared {clearedAll} subscriber(s) successfully");
        }

        var cleared = await _healthTracker.ClearAllUnhealthyAsync(CancellationToken.None, keys);
        var message =
            cleared > 0 ? $"Removed {cleared} subscriber(s) successfully" : "Selected subscriber(s) were no longer present (already cleared)";
        return (cleared, message);
    }

    private static string FormatDateTime(DateTime? dateTime)
    {
        if (!dateTime.HasValue)
            return "N/A";

        var utcTime = dateTime.Value;
        var timeAgo = DateTime.UtcNow - utcTime;

        string relativeTime = timeAgo.TotalSeconds switch
        {
            < 1 => "just now",
            < 60 => $"{(int)timeAgo.TotalSeconds}s ago",
            < 3600 => $"{(int)timeAgo.TotalMinutes}m ago",
            < 86400 => $"{(int)timeAgo.TotalHours}h ago",
            _ => $"{(int)timeAgo.TotalDays}d ago",
        };

        return $"{utcTime:yyyy-MM-dd HH:mm:ss}<br/>({relativeTime})";
    }
}
