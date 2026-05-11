# Live Monitoring Proposal

SignalR can be added later for dashboard refresh and conflict alerts only.

Allowed:

- Push queue count updates.
- Push new conflict notifications.
- Push batch status changes.
- Refresh charts.

Blocked:

- No ApplyMode execution through SignalR.
- No PowerShell invocation.
- No batch apply.
- No automatic retry loop.

Recommended first step: polling every 60 seconds from read-only JSON endpoints after security review.
