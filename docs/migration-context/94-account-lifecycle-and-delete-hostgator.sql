create table if not exists ModernClientCardStatus (
    CardID int not null primary key,
    BusinessID int not null,
    IsActive tinyint(1) not null default 1,
    DisabledAt datetime null,
    UpdatedAt datetime not null,
    UpdatedByBusinessID int null,
    index IX_ModernClientCardStatus_BusinessID (BusinessID),
    index IX_ModernClientCardStatus_IsActive (IsActive)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
