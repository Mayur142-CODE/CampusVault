-- Debug script to check actual submission statuses in database
USE CampusVaultDB;
GO

-- Check what tasks exist and their submission status
SELECT 
    t.TaskId,
    t.Title,
    t.Status AS TaskStatus,
    ts.SubmissionId,
    ts.Status AS SubmissionStatus,
    ts.SubmittedAt,
    ts.Score,
    CASE 
        WHEN ts.Status IS NULL THEN 'Pending'
        ELSE ts.Status 
    END AS DisplayStatus
FROM Tasks t
LEFT JOIN TaskSubmissions ts ON t.TaskId = ts.TaskId
WHERE t.Status = 'Active'
ORDER BY t.TaskId;

-- Check distinct submission status values
SELECT DISTINCT Status, COUNT(*) as Count
FROM TaskSubmissions
GROUP BY Status;
