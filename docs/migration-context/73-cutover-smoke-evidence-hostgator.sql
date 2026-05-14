create table if not exists ModernCutoverSmoke (
    ID bigint not null auto_increment,
    BusinessID int not null,
    AdminUserID int not null,
    HealthOk bit not null default b'0',
    ReadyOk bit not null default b'0',
    EmailOk bit not null default b'0',
    AppleWalletOk bit not null default b'0',
    GoogleWalletOk bit not null default b'0',
    ModernStampOk bit not null default b'0',
    SupportReviewed bit not null default b'0',
    Notes varchar(500) null,
    CreatedAt datetime not null,
    primary key (ID),
    index IX_ModernCutoverSmoke_Business_CreatedAt (BusinessID, CreatedAt),
    index IX_ModernCutoverSmoke_AdminUserID (AdminUserID)
) character set utf8mb4 collate utf8mb4_unicode_ci;
