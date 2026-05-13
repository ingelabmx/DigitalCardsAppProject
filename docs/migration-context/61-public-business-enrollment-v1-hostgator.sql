create table if not exists BusinessEnrollmentLinkToken (
    TokenHash char(64) not null,
    TokenSuffix varchar(16) not null,
    BusinessID int not null,
    CreatedAt datetime not null,
    LastUsedAt datetime null,
    RevokedAt datetime null,
    primary key (TokenHash),
    key IX_BusinessEnrollmentLinkToken_BusinessID_RevokedAt (BusinessID, RevokedAt),
    key IX_BusinessEnrollmentLinkToken_TokenSuffix (TokenSuffix)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
