create table if not exists ModernClientConsent (
    ID bigint not null auto_increment,
    UserID int not null,
    BusinessID int null,
    PolicyVersion varchar(64) not null,
    Source varchar(64) not null,
    AcceptedAt datetime not null,
    primary key (ID),
    index IX_ModernClientConsent_User_AcceptedAt (UserID, AcceptedAt),
    index IX_ModernClientConsent_Business_AcceptedAt (BusinessID, AcceptedAt),
    index IX_ModernClientConsent_PolicyVersion (PolicyVersion)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
