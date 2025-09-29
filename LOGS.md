# Logging Configuration for OneGround ZGW APIs

This document outlines the logging strategy for OneGround ZGW APIs, which is configured using Serilog. The goal is to produce structured, manageable log files suitable for both local debugging and shipping to a centralized logging platform like Kibana.

## Key Features

The configuration is designed to be robust and prevent common logging pitfalls, such as filling up disk space or creating unmanageably large files.

### 1. Daily Log Rotation

- **`rollingInterval: RollingInterval.Day`**

Logs are automatically rotated into a new file at the beginning of each day (00:00 UTC). This organizes logs into daily chronological chunks, with filenames containing the date (e.g., `service-name-20250926.json`).

### 2. File Size Limiting

- **`fileSizeLimitBytes: 100 * 1024 * 1024`**

To handle days with high log volume, each individual log file is capped at **100 MB**. If a file reaches this limit before the day ends, a new, sequentially numbered file is created (e.g., `service-name-20250926_001.json`).

### 3. Automatic Cleanup (Retention Policy)

- **`retainedFileCountLimit: 3`**

The system automatically manages disk space by keeping a maximum of the **3 most recent log files**. When a new file is created that would exceed this limit, the oldest log file is automatically deleted.

## Summary of Behavior

- **Trigger for New File:** A new file is created either at the start of a new day or when the current file hits 100 MB.
- **Maximum Local Storage:** The total disk space used by logs will not exceed approximately **300 MB** (100 MB per file x 3 files).
- **File Location:** `/var/log/app/{logFileName}` (Linux production environments only)
- **Format:** JSON with one object per line for structured logging

## Configuration Snippet

Here is the C# configuration code implementing this strategy:

```csharp
.WriteTo.Async(a =>
    a.File(
        formatter: new JsonFormatter(renderMessage: true, formatProvider: CultureInfo.CurrentCulture),
        path: logPath,
        fileSizeLimitBytes: 100 * 1024 * 1024,      // 100 MB
        rollOnFileSizeLimit: true,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 3,                  // Keep 3 files
        shared: true
    )
);
```

This setup provides a reliable and self-managing logging system that is safe for production environments using OneGround ZGW APIs.
