using Hangfire.Dashboard;

namespace OneGround.ZGW.Notificaties.Messaging.UI;

/// <summary>
/// Serves the client-side script for <see cref="UnhealthMonitorDashboardPage"/> as its own dashboard route,
/// so the page needs no inline script and satisfies a <c>script-src 'self'</c> Content-Security-Policy.
/// </summary>
public class UnhealthMonitorScriptPage : IDashboardDispatcher
{
    /// <summary>
    /// The dashboard-relative route this script is served from. Deliberately free of a '.' extension:
    /// Hangfire wraps a route template in ^...$ and matches it as a regular expression, so a literal dot
    /// would match any character.
    /// </summary>
    public const string RoutePath = "/unhealthmonitor/script";

    public async Task Dispatch(DashboardContext context)
    {
        context.Response.ContentType = "application/javascript";

        await context.Response.WriteAsync(Script);
    }

    private const string Script = """
        let refreshIntervalId = null;
        let pendingAction = null;

        function refreshTable() {
            fetch(window.location.href)
                .then(response => response.text())
                .then(html => {
                    const parser = new DOMParser();
                    const doc = parser.parseFromString(html, 'text/html');

                    const newSummary = doc.querySelector('.summary');
                    const currentSummary = document.querySelector('.summary');
                    if (newSummary && currentSummary) {
                        currentSummary.innerHTML = newSummary.innerHTML;
                    }

                    const newContent = doc.querySelector('#table-content');
                    const currentContent = document.querySelector('#table-content');
                    if (newContent && currentContent) {
                        currentContent.innerHTML = newContent.innerHTML;
                    }

                    const newTimestamp = doc.querySelector('.timestamp');
                    const currentTimestamp = document.querySelector('.timestamp');
                    if (newTimestamp && currentTimestamp) {
                        currentTimestamp.innerHTML = newTimestamp.innerHTML;
                    }
                })
                .catch(error => console.error('Error refreshing table:', error));
        }

        function toggleAutoRefresh() {
            const toggle = document.getElementById('autoRefreshToggle');
            const isEnabled = toggle.checked;

            if (isEnabled) {
                // Start auto-refresh every 3 seconds
                refreshIntervalId = setInterval(refreshTable, 3000);
                localStorage.setItem('autoRefreshEnabled', 'true');
            } else {
                // Stop auto-refresh
                if (refreshIntervalId) {
                    clearInterval(refreshIntervalId);
                    refreshIntervalId = null;
                }
                localStorage.setItem('autoRefreshEnabled', 'false');
            }
        }

        function initializeAutoRefresh() {
            const toggle = document.getElementById('autoRefreshToggle');
            const savedState = localStorage.getItem('autoRefreshEnabled');

            // Default to enabled if not set
            if (savedState === null || savedState === 'true') {
                toggle.checked = true;
                refreshIntervalId = setInterval(refreshTable, 3000);
            } else {
                toggle.checked = false;
            }
        }

        // Confirmation modal helpers
        function openConfirmModal(title, message, confirmLabel, action) {
            pendingAction = action;
            document.getElementById('modalTitle').textContent = title;
            document.getElementById('modalMessage').textContent = message;
            const confirmBtn = document.getElementById('modalConfirmBtn');
            confirmBtn.textContent = confirmLabel;
            confirmBtn.disabled = false;
            document.getElementById('confirmModal').classList.add('show');
            // Move keyboard focus into the dialog for accessibility
            confirmBtn.focus();
        }

        function closeConfirmModal() {
            pendingAction = null;
            document.getElementById('modalConfirmBtn').disabled = false;
            document.getElementById('confirmModal').classList.remove('show');
        }

        function confirmModalProceed() {
            const action = pendingAction;
            // Guard against rapid double-clicks: ignore if there is no pending action
            if (typeof action !== 'function') {
                return;
            }
            pendingAction = null;
            document.getElementById('modalConfirmBtn').disabled = true;
            document.getElementById('confirmModal').classList.remove('show');
            action();
        }

        // Ask for confirmation before clearing the whole cache
        function clearCache() {
            openConfirmModal(
                'Clear all unhealthy subscribers',
                'Are you sure you want to clear the cache for ALL unhealthy subscribers? This cannot be undone.',
                'Clear all',
                submitClearAllRequest
            );
        }

        // Shared POST helper - returns the parsed JSON response
        async function postClearRequest(body) {
            const response = await fetch(window.location.href, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded'
                },
                body: body
            });
            return response.json();
        }

        async function submitClearAllRequest() {
            const btn = document.getElementById('clearCacheBtn');
            btn.disabled = true;
            btn.textContent = 'Clearing...';

            try {
                const data = await postClearRequest('');
                if (data.success) {
                    showNotification(data.message || 'Cache cleared successfully');
                    setTimeout(refreshTable, 500);
                } else {
                    showNotification(data.message || 'Failed to clear cache', true);
                }
            } catch (error) {
                console.error('Error clearing cache:', error);
                showNotification('Error clearing cache: ' + error.message, true);
            } finally {
                btn.disabled = false;
                btn.textContent = 'Clear all';
            }
        }

        // Ask for confirmation before deleting a single subscriber
        function deleteItem(button) {
            const key = button.getAttribute('data-key');
            const url = button.getAttribute('data-url') || 'this subscriber';
            openConfirmModal(
                'Delete subscriber',
                'Are you sure you want to remove ' + url + ' from the unhealthy cache?',
                'Delete',
                () => submitDeleteRequest(key, button)
            );
        }

        function resetDeleteButton(button) {
            if (button) {
                button.disabled = false;
                button.textContent = 'Delete';
            }
        }

        async function submitDeleteRequest(key, button) {
            if (button) {
                button.disabled = true;
                button.textContent = 'Deleting...';
            }

            try {
                const data = await postClearRequest('key=' + encodeURIComponent(key));
                if (data.success) {
                    showNotification(data.message || 'Subscriber removed successfully');
                    // On success the row disappears after the refresh, so no button reset needed
                    setTimeout(refreshTable, 500);
                } else {
                    showNotification(data.message || 'Failed to remove subscriber', true);
                    resetDeleteButton(button);
                }
            } catch (error) {
                console.error('Error removing subscriber:', error);
                showNotification('Error removing subscriber: ' + error.message, true);
                resetDeleteButton(button);
            }
        }

        function showNotification(message, isError = false) {
            const notification = document.getElementById('notification');
            notification.textContent = message;
            notification.style.backgroundColor = isError ? '#dc3545' : '#28a745';
            notification.classList.add('show');

            setTimeout(() => {
                notification.classList.remove('show');
            }, 3000);
        }

        // Replaces the inline on* attributes the page used to carry, so no inline script is needed.
        function bindEventHandlers() {
            document.getElementById('autoRefreshToggle').addEventListener('change', toggleAutoRefresh);
            document.getElementById('clearCacheBtn').addEventListener('click', clearCache);
            document.getElementById('modalCancelBtn').addEventListener('click', closeConfirmModal);
            document.getElementById('modalConfirmBtn').addEventListener('click', confirmModalProceed);
            document.getElementById('backLink').addEventListener('click', event => {
                event.preventDefault();
                history.back();
            });

            // refreshTable() replaces everything inside #table-content, so a per-button Delete handler
            // would not survive the first auto-refresh. Delegate from the container, which does survive.
            document.getElementById('table-content').addEventListener('click', event => {
                const button = event.target.closest('.delete-btn');
                if (button) {
                    deleteItem(button);
                }
            });
        }

        function initialize() {
            bindEventHandlers();
            initializeAutoRefresh();
        }

        // Initialize on page load
        window.addEventListener('DOMContentLoaded', initialize);
        """;
}
