-- Stamp Ledger v1
-- Ejecutar manualmente en HostGator antes de activar este PR contra MySQL real.
-- No modifica tablas legacy ni hace backfill historico.

create table if not exists StampLedger (
    ID int(11) not null auto_increment,
    CardID int(11) not null,
    BusinessID int(11) not null,
    UserID int(11) not null,
    Source varchar(32) not null,
    ActorBusinessID int(11) null,
    PreviousCheckQTY int(11) not null,
    NewCheckQTY int(11) not null,
    PreviousHistoricCheckQTY int(11) not null,
    NewHistoricCheckQTY int(11) not null,
    ObservedLastCheck datetime(6) not null,
    GoogleWalletAttempted tinyint(1) not null default 0,
    GoogleWalletSucceeded tinyint(1) not null default 0,
    AppleWalletAttempted tinyint(1) not null default 0,
    AppleWalletSucceeded tinyint(1) not null default 0,
    ErrorSummary varchar(255) null,
    CreatedAt datetime(6) not null,
    primary key (ID),
    key IX_StampLedger_CardID_CreatedAt (CardID, CreatedAt),
    key IX_StampLedger_BusinessID_CreatedAt (BusinessID, CreatedAt),
    key IX_StampLedger_Source_CreatedAt (Source, CreatedAt)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
