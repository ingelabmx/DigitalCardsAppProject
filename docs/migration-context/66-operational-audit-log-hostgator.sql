create table if not exists ModernAuditEvent (
    ID bigint not null auto_increment,
    EventType varchar(64) not null,
    ActorAdminUserID int not null,
    BusinessID int null,
    UserID int null,
    CardID int null,
    TargetAdminUserID int null,
    Summary varchar(500) not null,
    CreatedAt datetime not null,
    primary key (ID),
    index IX_ModernAuditEvent_EventType_CreatedAt (EventType, CreatedAt),
    index IX_ModernAuditEvent_Actor_CreatedAt (ActorAdminUserID, CreatedAt),
    index IX_ModernAuditEvent_Business_CreatedAt (BusinessID, CreatedAt),
    index IX_ModernAuditEvent_User_CreatedAt (UserID, CreatedAt),
    index IX_ModernAuditEvent_Card_CreatedAt (CardID, CreatedAt)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
