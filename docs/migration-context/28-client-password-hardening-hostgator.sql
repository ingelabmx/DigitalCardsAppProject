-- Client Password Hardening v1
-- Ejecutar manualmente en HostGator antes de usar hashes modernos de cliente.
-- No modifica tablas legacy ni rompe Web Forms.

create table if not exists ModernClientCredential (
    UserID int(11) not null,
    PasswordHash varchar(512) not null,
    CreatedAt datetime(6) not null,
    UpdatedAt datetime(6) not null,
    primary key (UserID),
    constraint ModernClientCredential_ibfk_1
        foreign key (UserID) references UserClient (UserID)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
