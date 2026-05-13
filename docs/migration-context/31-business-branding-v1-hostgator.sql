-- Business Branding v1
-- Ejecutar manualmente en HostGator antes de editar branding real desde /Admin/BusinessProfile.
-- No modifica tablas legacy ni rompe Web Forms.

create table if not exists ModernBusinessBranding (
    BusinessID int(11) not null,
    PublicName varchar(80) not null,
    LogoPath varchar(200) not null,
    PrimaryColor varchar(7) not null,
    SecondaryColor varchar(7) not null,
    ProgramName varchar(80) not null,
    ProgramDescription varchar(280) not null,
    UpdatedAt datetime(6) not null,
    UpdatedByAdminUserID int(11) not null,
    primary key (BusinessID),
    key IX_ModernBusinessBranding_UpdatedAt (UpdatedAt),
    constraint ModernBusinessBranding_Business_fk
        foreign key (BusinessID) references Business (BusinessID),
    constraint ModernBusinessBranding_Admin_fk
        foreign key (UpdatedByAdminUserID) references UserClient (UserID)
) engine=InnoDB default charset=utf8mb4 collate=utf8mb4_unicode_ci;
