This document provides comprehensive guidance on disaster recovery procedures for the Security Patrol application. It covers recovery objectives, disaster scenarios, recovery procedures, and testing methodologies to ensure business continuity in the event of system failures or disasters.

Disaster recovery is a critical aspect of the Security Patrol application's operational resilience, ensuring that the system can recover from various failure scenarios with minimal data loss and downtime. This document serves as a reference for operations teams, developers, and stakeholders responsible for maintaining the application's availability and reliability.

## Recovery Objectives
The Security Patrol application has defined recovery objectives that guide the disaster recovery strategy and procedures.

### Recovery Time Objective (RTO)
The Recovery Time Objective (RTO) defines the maximum acceptable time to restore service after a disaster:

| Environment | RTO | Description |
|------------|-----|-------------|
| Production | 4 hours | Maximum time to restore critical functionality |
| Staging | 8 hours | Maximum time to restore staging environment |
| Development/Testing | 24 hours | Maximum time to restore non-production environments |

The RTO is measured from the time a disaster is declared until the time the application is available to users with critical functionality restored. Different components may have different recovery priorities within the overall RTO.

### Recovery Point Objective (RPO)
The Recovery Point Objective (RPO) defines the maximum acceptable data loss in the event of a disaster:

| Environment | RPO | Description |
|------------|-----|-------------|
| Production | 1 hour | Maximum acceptable data loss |
| Staging | 4 hours | Maximum acceptable data loss |
| Development/Testing | 24 hours | Maximum acceptable data loss |

The RPO is achieved through a combination of database replication, transaction log backups, and blob storage redundancy. Different data types may have different RPOs based on their criticality.

### Service Level Objectives
In addition to RTO and RPO, the following service level objectives apply during disaster recovery:

| Metric | Target | Description |
|--------|--------|-------------|
| Authentication Success Rate | 99.9% | Percentage of successful authentication attempts |
| API Availability | 99.5% | Percentage of successful API requests |
| Mobile App Functionality | 95% | Percentage of core features available |
| Data Integrity | 100% | Percentage of data maintained without corruption |

These objectives guide the prioritization of recovery efforts and help measure the success of the disaster recovery process.

### Recovery Priorities
In the event of a disaster, system components will be recovered in the following order of priority:

1. **Authentication Services**: Enable users to log in
2. **Core Database**: Restore essential data
3. **API Services**: Enable basic mobile app functionality
4. **Blob Storage**: Restore access to photos and files
5. **Background Services**: Enable data synchronization
6. **Monitoring and Alerting**: Restore visibility into system health
7. **Non-critical Features**: Restore remaining functionality

This prioritization ensures that the most critical functionality is restored first, allowing users to continue essential security operations even during the recovery process.

## Disaster Scenarios
This section describes the potential disaster scenarios that could affect the Security Patrol application and their impact on system availability and data integrity.

### Single Service Failure
A single service failure occurs when an individual component of the system fails while the rest of the system remains operational.

**Potential Causes:**
- Application code errors
- Resource exhaustion (memory, CPU, connections)
- Configuration errors
- Dependency failures

**Impact:**
- Partial loss of functionality
- Degraded performance
- Potential data processing delays

**Detection Methods:**
- Service health alerts
- Application Insights availability tests
- Error rate monitoring
- User-reported issues

**Recovery Approach:**
- Automatic failover for stateless services
- Service restart or redeployment
- Configuration correction
- Dependency restoration

Single service failures are typically the most common type of failure and are usually resolved through automated recovery mechanisms or simple manual interventions.

### Region Outage
A region outage occurs when an entire Azure region becomes unavailable, affecting all services deployed in that region.

**Potential Causes:**
- Azure regional service disruption
- Network connectivity issues
- Natural disasters
- Power outages

**Impact:**
- Complete loss of service in the affected region
- Potential data loss for in-flight transactions
- Mobile app connectivity issues

**Detection Methods:**
- Azure Service Health alerts
- Multi-region availability monitoring
- External monitoring services
- Azure status page

**Recovery Approach:**
- Failover to secondary region
- Traffic Manager rerouting
- Geo-replicated database failover
- Cross-region storage access

The Security Patrol application is designed with multi-region resilience for production environments, allowing for continued operation during a region outage with minimal service disruption.

### Data Corruption
Data corruption occurs when data in the database or storage becomes corrupted, potentially affecting data integrity and application functionality.

**Potential Causes:**
- Software bugs
- Failed updates or migrations
- Storage hardware issues
- Malicious activities

**Impact:**
- Incorrect application behavior
- Data integrity issues
- Potential service disruption
- User data loss or inconsistency

**Detection Methods:**
- Data validation checks
- Database integrity monitoring
- Application error monitoring
- User-reported data issues

**Recovery Approach:**
- Point-in-time database restore
- Data recovery from backups
- Corruption isolation and repair
- Data reconciliation procedures

Data corruption scenarios require careful handling to ensure that the recovery process does not propagate corrupted data or cause additional data loss.

### Complete System Failure
A complete system failure occurs when multiple critical components fail simultaneously, resulting in a total loss of service.

**Potential Causes:**
- Multiple region outages
- Catastrophic deployment errors
- Security incidents
- Critical dependency failures

**Impact:**
- Complete service unavailability
- Potential data loss
- Extended recovery time
- Mobile app unusable

**Detection Methods:**
- Multiple critical alerts
- Complete loss of monitoring data
- External monitoring services
- User-reported complete outage

**Recovery Approach:**
- Full system recovery from backups
- Infrastructure rebuilding using Terraform
- Database restoration from geo-replicas or backups
- Phased service restoration based on priorities

Complete system failures are rare but require the most comprehensive recovery procedures and typically involve the longest recovery times.

### Mobile App Issues
Mobile app issues occur when the mobile application experiences problems that affect user experience or functionality.

**Potential Causes:**
- Application bugs
- API compatibility issues
- Device-specific problems
- Network connectivity issues

**Impact:**
- Degraded mobile app functionality
- User experience issues
- Synchronization failures
- Potential data collection issues

**Detection Methods:**
- App Center crash reports
- User-reported issues
- API error monitoring
- Synthetic transaction monitoring

**Recovery Approach:**
- Server-side feature toggles
- Emergency app updates
- API compatibility adjustments
- User communication and workarounds

Mobile app issues may require different recovery approaches than backend issues, as deploying fixes to mobile devices involves additional complexity and user involvement.

### Security Incidents
Security incidents involve unauthorized access, data breaches, or other security-related events that compromise the system's integrity or confidentiality.

**Potential Causes:**
- Unauthorized access attempts
- Credential compromise
- Vulnerabilities in application code
- Insider threats

**Impact:**
- Data confidentiality breaches
- System integrity issues
- Potential service disruption
- Regulatory and compliance implications

**Detection Methods:**
- Security monitoring alerts
- Unusual access patterns
- Authentication anomalies
- Vulnerability scanning

**Recovery Approach:**
- Incident containment procedures
- Security breach remediation
- Credential rotation and access revocation
- System integrity verification

Security incidents require coordination with the security team and may involve additional reporting and compliance requirements beyond standard disaster recovery procedures.

## Recovery Procedures
This section details the procedures for recovering from various disaster scenarios, ensuring that the system can be restored to normal operation within the defined RTO and RPO.

### Disaster Recovery Team
The Disaster Recovery Team is responsible for executing recovery procedures and coordinating the response to disaster events.

**Team Composition:**

| Role | Responsibilities | Primary Contact | Secondary Contact |
|------|-----------------|-----------------|-------------------|
| DR Coordinator | Overall coordination and communication | dr-coordinator@example.com | +1-555-123-4567 |
| Infrastructure Lead | Infrastructure recovery and provisioning | infra-lead@example.com | +1-555-123-4568 |
| Database Administrator | Database recovery and data integrity | dba@example.com | +1-555-123-4569 |
| Application Lead | Application deployment and verification | app-lead@example.com | +1-555-123-4570 |
| Security Officer | Security assessment and remediation | security@example.com | +1-555-123-4571 |
| Communications Lead | Stakeholder and user communications | comms@example.com | +1-555-123-4572 |

**Activation Process:**

1. Incident detection through monitoring alerts or manual reporting
2. Initial assessment by on-call engineer
3. Escalation to DR Coordinator if disaster criteria met
4. DR Coordinator activates the Disaster Recovery Team
5. Team assembles in the designated communication channel
6. DR Coordinator assigns responsibilities based on the scenario
7. Recovery procedures initiated according to the disaster type

The Disaster Recovery Team should conduct regular drills and training to ensure readiness for actual disaster events.

### Incident Detection and Classification
Proper incident detection and classification is critical for initiating the appropriate recovery procedures.

**Detection Mechanisms:**

- **Automated Monitoring**: Azure Monitor alerts, Application Insights, App Center
- **Manual Reporting**: User reports, operations team observations
- **External Notifications**: Azure Service Health, vendor notifications

**Classification Criteria:**

| Severity | Criteria | Response Time | Escalation Path |
|----------|----------|---------------|----------------|
| Critical (Disaster) | Complete service outage, data loss risk, security breach | Immediate | DR Coordinator → Full DR Team |
| High | Major functionality affected, significant performance degradation | 15 minutes | On-call Engineer → Service Owner → DR Coordinator |
| Medium | Limited functionality affected, moderate performance issues | 30 minutes | On-call Engineer → Service Owner |
| Low | Minor issues, minimal user impact | 2 hours | On-call Engineer |

**Classification Process:**

1. Initial assessment of incident scope and impact
2. Determination of affected components and services
3. Evaluation against classification criteria
4. Assignment of severity level
5. Escalation according to severity
6. Continuous reassessment as more information becomes available

Only incidents classified as Critical (Disaster) trigger the full disaster recovery procedures. Lower severity incidents follow standard incident management processes.

### Single Service Recovery
Procedures for recovering from single service failures:

**1. API Service Recovery:**

```bash
# Restart the App Service
az webapp restart --resource-group securitypatrol-prod-rg --name securitypatrol-api

# If restart doesn't resolve the issue, redeploy the latest known good version
az webapp deployment source config-zip --resource-group securitypatrol-prod-rg --name securitypatrol-api --src ./artifacts/api-backup.zip

# Check service health after recovery
az webapp show --resource-group securitypatrol-prod-rg --name securitypatrol-api --query "state"
```

**2. Database Service Recovery:**

```bash
# Check database status
az sql db show --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db --query "status"

# If database is unavailable, check for service issues
az sql server show --resource-group securitypatrol-prod-rg --name securitypatrol-sql --query "state"

# If necessary, failover to geo-replicated secondary
az sql db failover --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db
```

**3. Storage Service Recovery:**

```bash
# Check storage account status
az storage account show --resource-group securitypatrol-prod-rg --name securitypatrolstorage --query "statusOfPrimary"

# If storage is unavailable, check for service issues
az storage account show --resource-group securitypatrol-prod-rg --name securitypatrolstorage --expand geoReplicationStats

# If necessary, initiate failover to secondary region
az storage account failover --resource-group securitypatrol-prod-rg --name securitypatrolstorage
```

**Recovery Verification:**

1. Verify service health endpoints
2. Check application logs for errors
3. Test basic functionality
4. Monitor performance metrics
5. Verify data integrity

Single service recovery should be attempted before escalating to more comprehensive recovery procedures.

### Regional Failover
Procedures for recovering from a region outage by failing over to the secondary region:

**1. Assess Region Status:**

```bash
# Check Azure Service Health for region status
az monitor activity-log list --resource-group securitypatrol-prod-rg --filter "eventTimestamp ge '$(date -u -d '-1 hour' +'%Y-%m-%dT%H:%M:%SZ')' and level eq 'Error' or level eq 'Critical'"
```

**2. Initiate Traffic Manager Failover:**

```bash
# Check Traffic Manager endpoint status
az network traffic-manager endpoint show --resource-group securitypatrol-prod-rg --profile-name securitypatrol-tm --type azureEndpoints --name primary

# Disable primary endpoint to force traffic to secondary
az network traffic-manager endpoint update --resource-group securitypatrol-prod-rg --profile-name securitypatrol-tm --type azureEndpoints --name primary --endpoint-status Disabled
```

**3. Database Failover:**

```bash
# Initiate database failover to secondary region
az sql db failover --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db --allow-data-loss
```

**4. Storage Failover:**

```bash
# Initiate storage account failover
az storage account failover --resource-group securitypatrol-prod-rg --name securitypatrolstorage
```

**5. Verify Secondary Region Services:**

```bash
# Check secondary API service health
az webapp show --resource-group securitypatrol-prod-rg --name securitypatrol-api-secondary --query "state"

# Check secondary database status
az sql db show --resource-group securitypatrol-prod-rg --server securitypatrol-sql-secondary --name securitypatrol-db --query "status"
```

**6. Update Mobile App Configuration:**

If necessary, update mobile app configuration to point to secondary region endpoints. This may be handled automatically by Traffic Manager if the app is configured to use the Traffic Manager DNS name.

**Recovery Verification:**

1. Verify Traffic Manager routing to secondary region
2. Test API functionality through secondary endpoint
3. Verify database read/write operations
4. Test mobile app connectivity
5. Monitor performance metrics in secondary region

Regional failover should be tested regularly to ensure it functions correctly when needed.

### Data Recovery
Procedures for recovering from data corruption or loss:

**1. Database Recovery:**

The following procedures detail how to recover the database from corruption or data loss:

```bash
# Identify the point-in-time to restore to (before corruption)
az sql db list-deleted --resource-group securitypatrol-prod-rg --server securitypatrol-sql

# Restore database to point-in-time
az sql db restore --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db --dest-name securitypatrol-db-restored --time "2023-07-15T13:10:00Z"

# Verify restored database
az sql db show --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db-restored --query "status"

# After verification, rename databases to swap
az sql db rename --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db --new-name securitypatrol-db-old
az sql db rename --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db-restored --new-name securitypatrol-db
```

Additional database recovery options include:

- Geo-restore from a geo-replicated database
- Restore from a long-term retention backup
- Export/Import using bacpac files

**2. Blob Storage Recovery:**

The following procedures detail how to recover blob storage data:

```bash
# Restore from soft delete (if within retention period)
az storage blob undelete --account-name securitypatrolstorage --container-name photos --name "patrol-photos/2023/07/15/photo1.jpg"

# Restore from snapshot
az storage blob copy start --account-name securitypatrolstorage --container-name photos --name "patrol-photos/2023/07/15/photo1.jpg" --source-container photos --source-blob "patrol-photos/2023/07/15/photo1.jpg" --source-snapshot "2023-07-15T10:30:00.0000000Z"

# Restore from backup container
az storage blob copy start --account-name securitypatrolstorage --container-name photos --name "patrol-photos/2023/07/15/photo1.jpg" --source-account-name securitypatrolbackup --source-container photos-backup --source-blob "patrol-photos/2023/07/15/photo1.jpg"
```

Blob storage recovery options include:

- Soft delete recovery (undelete)
- Point-in-time restore using snapshots
- AzCopy to restore from backup storage account
- Container recovery using Azure Portal

**3. Data Integrity Verification:**

After data recovery, it's essential to verify data integrity:

```sql
-- Check database integrity
DBCC CHECKDB (N'securitypatrol-db') WITH NO_INFOMSGS, ALL_ERRORMSGS;

-- Verify record counts
SELECT 'Users' AS Table, COUNT(*) AS RecordCount FROM Users
UNION ALL
SELECT 'TimeRecords', COUNT(*) FROM TimeRecords
UNION ALL
SELECT 'LocationRecords', COUNT(*) FROM LocationRecords
UNION ALL
SELECT 'Reports', COUNT(*) FROM Reports;

-- Check most recent data
SELECT MAX(Timestamp) AS LatestTimeRecord FROM TimeRecords;
SELECT MAX(Timestamp) AS LatestLocationRecord FROM LocationRecords;
```

**4. Application Data Reconciliation:**

If data recovery results in inconsistencies between related data, reconciliation may be necessary:

1. Identify inconsistencies through data validation queries
2. Apply business rules to resolve inconsistencies
3. Document all reconciliation actions
4. Verify application functionality with reconciled data

Data recovery should prioritize maintaining data integrity and consistency across all system components.

### Complete System Recovery
Procedures for recovering from a complete system failure:

**1. Infrastructure Rebuilding:**

Use Terraform to rebuild the infrastructure:

```bash
# Navigate to the Terraform directory
cd infrastructure/terraform

# Initialize Terraform
terraform init

# Select the production workspace
terraform workspace select prod

# Apply the Terraform configuration
terraform apply -var-file=environments/prod/terraform.tfvars
```

For detailed infrastructure deployment procedures, see the [Deployment](./deployment.md#infrastructure-deployment) documentation.

**2. Database Recovery:**

Restore the database from the most recent backup:

```bash
# Restore database from backup
az sql db import --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db --storage-key-type StorageAccessKey --storage-key "<storage-access-key>" --storage-uri "https://securitypatrolbackup.blob.core.windows.net/database-backups/securitypatrol-db-backup-20230715.bacpac" --admin-user "<admin-username>" --admin-password "<admin-password>"
```

**3. Backend Deployment:**

Deploy the backend services:

```bash
# Deploy backend services
cd infrastructure/scripts
./deploy-backend.sh -e prod -t v1.0.0
```

For detailed backend deployment procedures, see the [Deployment](./deployment.md#backend-deployment) documentation.

**4. Storage Recovery:**

Restore blob storage data from backups:

```bash
# Use AzCopy to restore from backup storage account
azcopy copy "https://securitypatrolbackup.blob.core.windows.net/photos-backup/*" "https://securitypatrolstorage.blob.core.windows.net/photos/" --recursive
```

**5. Configuration Restoration:**

Restore application configuration:

```bash
# Restore Key Vault secrets
az keyvault restore --vault-name securitypatrol-kv --storage-account-name securitypatrolbackup --storage-container-name keyvault-backups --name keyvault-backup-20230715

# Restore App Service configuration
az webapp config appsettings set --resource-group securitypatrol-prod-rg --name securitypatrol-api --settings @appsettings.json
```

**6. Monitoring Restoration:**

Restore monitoring configuration:

```bash
# Set up monitoring
cd infrastructure/scripts
./setup-monitoring.ps1 -Environment prod -ResourceGroupName securitypatrol-prod-rg -AppServiceName securitypatrol-api
```

**7. System Verification:**

After all components are restored, perform comprehensive verification:

1. Verify infrastructure provisioning
2. Check database connectivity and data integrity
3. Test API functionality
4. Verify storage access
5. Test end-to-end user flows
6. Confirm monitoring is operational

Complete system recovery is the most complex recovery scenario and should be practiced regularly through disaster recovery drills.

### Mobile App Recovery
Procedures for recovering from mobile app issues:

**1. Server-Side Feature Toggles:**

Use feature toggles to disable problematic features without requiring app updates:

```bash
# Update feature flags in App Service configuration
az webapp config appsettings set --resource-group securitypatrol-prod-rg --name securitypatrol-api --settings "FeatureFlags:EnablePatrolVerification=false"
```

**2. API Compatibility Adjustments:**

If mobile app issues are caused by API compatibility problems, adjust the API to maintain compatibility:

```bash
# Deploy API version with compatibility fixes
cd infrastructure/scripts
./deploy-backend.sh -e prod -t v1.0.1-compat
```

**3. Emergency App Update:**

If server-side mitigations are insufficient, prepare and release an emergency app update:

1. Fix the issues in the mobile app code
2. Build and sign an emergency release
3. Publish to Google Play with expedited review request
4. Use staged rollout to monitor impact

**4. User Communication:**

Communicate with users about the issues and recovery status:

1. In-app notifications for known issues
2. Email communications for critical updates
3. Instructions for workarounds if available
4. Estimated timeline for resolution

**5. Verification:**

After recovery actions, verify mobile app functionality:

1. Test on multiple device types and OS versions
2. Verify core functionality (authentication, clock in/out, patrol management)
3. Monitor crash reports and error rates
4. Collect user feedback on the resolution

Mobile app recovery may require coordination with app store review processes, which can impact recovery timelines.

### Security Incident Recovery
Procedures for recovering from security incidents:

**1. Containment:**

```bash
# Revoke compromised access tokens
az webapp auth update --resource-group securitypatrol-prod-rg --name securitypatrol-api --reset-auth-api-key

# Disable external access temporarily if needed
az webapp config access-restriction add --resource-group securitypatrol-prod-rg --name securitypatrol-api --rule-name "EmergencyLockdown" --action Deny --ip-address "0.0.0.0/0" --priority 100
```

**2. Credential Rotation:**

```bash
# Rotate database credentials
az sql server update --resource-group securitypatrol-prod-rg --name securitypatrol-sql --admin-password "<new-password>"

# Update connection string in Key Vault
az keyvault secret set --vault-name securitypatrol-kv --name "DatabaseConnection" --value "Server=tcp:securitypatrol-sql.database.windows.net,1433;Initial Catalog=securitypatrol-db;User ID=sqladmin;Password=<new-password>;Encrypt=true;Connection Timeout=30;"

# Rotate storage access keys
az storage account keys renew --resource-group securitypatrol-prod-rg --account-name securitypatrolstorage --key primary

# Update storage key in Key Vault
az keyvault secret set --vault-name securitypatrol-kv --name "StorageKey" --value "$(az storage account keys list --resource-group securitypatrol-prod-rg --account-name securitypatrolstorage --query '[0].value' -o tsv)"
```

**3. System Integrity Verification:**

```bash
# Verify deployment integrity
az webapp deployment list --resource-group securitypatrol-prod-rg --name securitypatrol-api

# Check for unauthorized changes
az resource list --resource-group securitypatrol-prod-rg --query "[].{Name:name, Type:type, LastModified:tags.lastModified}"
```

**4. Vulnerability Remediation:**

1. Apply security patches to affected components
2. Update dependencies with known vulnerabilities
3. Implement additional security controls
4. Conduct security scanning to verify remediation

**5. Forensic Analysis:**

1. Preserve evidence for investigation
2. Analyze logs for unauthorized access patterns
3. Identify the attack vector and exploitation method
4. Document findings for future prevention

**6. Reporting and Compliance:**

1. Document the incident details and response actions
2. Prepare reports for regulatory requirements
3. Notify affected parties if required by regulations
4. Update security procedures based on lessons learned

Security incident recovery should be coordinated with the security team and follow the organization's security incident response plan.

### Communication Plan
Effective communication is critical during disaster recovery to keep stakeholders informed and coordinate recovery efforts.

**1. Internal Communication:**

| Audience | Communication Channel | Frequency | Content |
|----------|------------------------|-----------|--------|
| DR Team | Teams/Slack channel | Continuous during recovery | Technical details, tasks, progress |
| IT Management | Email + Conference call | Hourly during critical phase | Recovery status, resource needs, timeline |
| Executive Team | Email + Dashboard | At key milestones | Business impact, high-level status, ETA |
| All Staff | Email + Intranet | Daily | Service status, workarounds, expectations |

**2. External Communication:**

| Audience | Communication Channel | Frequency | Content |
|----------|------------------------|-----------|--------|
| End Users | In-app notifications, Email | At key milestones | Service status, workarounds, ETA |
| Clients/Partners | Email, Support portal | Daily | Impact summary, recovery progress, alternatives |
| Regulators | Formal notification | As required by regulations | Incident details, impact assessment, recovery plan |
| Public | Website status page | As needed | Service status, expected resolution time |

**3. Communication Templates:**

*Initial Notification:*
```
Subject: [ALERT] Security Patrol Application Service Disruption

We are currently experiencing a service disruption affecting the Security Patrol application. Our technical team has been notified and is actively working to resolve the issue.

Impact: [describe affected functionality]
Estimated Resolution Time: [initial estimate]
Workarounds: [if available]

We will provide updates as more information becomes available.
```

*Status Update:*
```
Subject: [UPDATE] Security Patrol Application Service Disruption

Our team continues to work on resolving the current service disruption.

Current Status: [description of progress]
Remaining Issues: [description of outstanding issues]
Updated Resolution Time: [revised estimate]
Workarounds: [updated if available]

Next update will be provided in [timeframe].
```

*Resolution Notification:*
```
Subject: [RESOLVED] Security Patrol Application Service Restored

The service disruption affecting the Security Patrol application has been resolved.

Root Cause: [brief description]
Resolution: [actions taken]
Preventive Measures: [future prevention steps]

Please contact support if you continue to experience any issues.
```

**4. Communication Roles and Responsibilities:**

| Role | Responsibilities |
|------|------------------|
| DR Coordinator | Approve all external communications, coordinate with executive team |
| Communications Lead | Draft communications, manage distribution, monitor feedback |
| Technical Lead | Provide accurate technical details for communications |
| Support Team | Handle user inquiries, provide workarounds, collect feedback |

All communications should be clear, concise, and provide actionable information appropriate to the audience.

### Recovery Completion
The recovery process is not complete until all post-recovery activities have been performed and normal operations have been fully restored.

**1. Recovery Validation:**

Before declaring recovery complete, validate the following:

- All critical services are operational
- Data integrity has been verified
- Performance meets or exceeds baselines
- Security controls are fully functional
- Monitoring and alerting are operational
- User access is fully restored

**2. Post-Recovery Tasks:**

```bash
# Verify all services are running
az resource list --resource-group securitypatrol-prod-rg --query "[].{Name:name, Type:type, ProvisioningState:properties.provisioningState}"

# Check API health
curl -s -o /dev/null -w "%{http_code}" https://securitypatrol-api.azurewebsites.net/health

# Verify database connectivity
az sql db show --resource-group securitypatrol-prod-rg --server securitypatrol-sql --name securitypatrol-db --query "status"

# Check monitoring status
az monitor activity-log list --resource-group securitypatrol-prod-rg --filter "eventTimestamp ge '$(date -u -d '-1 hour' +'%Y-%m-%dT%H:%M:%SZ')'"