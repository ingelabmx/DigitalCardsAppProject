-- PR 33: Password Reset Flows v1
-- Apply manually in HostGator before enabling real password reset flows.
-- Stores only SHA-256 token hashes, never plaintext reset tokens.

create table if not exists ModernPasswordResetToken (
    ID bigint not null auto_increment,
    AccountType varchar(20) not null,
    AccountID int not null,
    TokenHash char(64) not null,
    TokenSuffix varchar(12) not null,
    CreatedAt datetime not null,
    ExpiresAt datetime not null,
    UsedAt datetime null,
    RevokedAt datetime null,
    primary key (ID),
    unique key UX_ModernPasswordResetToken_TokenHash_AccountType (TokenHash, AccountType),
    key IX_ModernPasswordResetToken_Account (AccountType, AccountID, UsedAt, RevokedAt, ExpiresAt),
    key IX_ModernPasswordResetToken_TokenSuffix (TokenSuffix)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
