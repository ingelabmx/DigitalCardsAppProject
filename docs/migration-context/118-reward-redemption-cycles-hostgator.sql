-- PR 118: Reward Redemption Cycles v1
-- Apply manually in HostGator before enabling reward redemption in production.

create table if not exists RewardRedemption (
    ID bigint not null auto_increment,
    CardID int not null,
    BusinessID int not null,
    UserID int not null,
    ActorBusinessID int null,
    StampGoal int not null,
    RedeemedCheckQTY int not null,
    HistoricCheckQTY int not null,
    RewardText varchar(280) not null,
    GoogleWalletAttempted tinyint(1) not null default 0,
    GoogleWalletSucceeded tinyint(1) not null default 0,
    AppleWalletAttempted tinyint(1) not null default 0,
    AppleWalletSucceeded tinyint(1) not null default 0,
    ErrorSummary varchar(255) null,
    RedeemedAt datetime(6) not null,
    CreatedAt datetime(6) not null,
    primary key (ID),
    key IX_RewardRedemption_CardID_RedeemedAt (CardID, RedeemedAt),
    key IX_RewardRedemption_BusinessID_RedeemedAt (BusinessID, RedeemedAt),
    key IX_RewardRedemption_UserID_RedeemedAt (UserID, RedeemedAt)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
