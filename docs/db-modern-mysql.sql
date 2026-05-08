create table if not exists modern_clients (
    id char(36) not null,
    user_name varchar(64) not null,
    first_name varchar(100) not null,
    last_name varchar(100) not null,
    email varchar(256) not null,
    created_at datetime(6) not null default current_timestamp(6),
    primary key (id),
    unique key ux_modern_clients_user_name (user_name),
    unique key ux_modern_clients_email (email)
);

create table if not exists modern_businesses (
    id char(36) not null,
    name varchar(120) not null,
    email varchar(256) not null,
    password_hash_placeholder varchar(256) not null,
    logo_path varchar(512) not null,
    created_at datetime(6) not null default current_timestamp(6),
    primary key (id),
    unique key ux_modern_businesses_email (email)
);

create table if not exists modern_loyalty_cards (
    id char(36) not null,
    client_id char(36) not null,
    business_id char(36) not null,
    enrollment_token varchar(64) not null,
    current_stamps int not null,
    lifetime_stamps int not null,
    created_at datetime(6) not null,
    last_stamped_at datetime(6) not null,
    google_object_id varchar(256) null,
    google_save_url varchar(2048) null,
    primary key (id),
    unique key ux_modern_loyalty_cards_token (enrollment_token),
    unique key ux_modern_loyalty_cards_client_business (client_id, business_id),
    constraint fk_modern_loyalty_cards_client foreign key (client_id) references modern_clients (id),
    constraint fk_modern_loyalty_cards_business foreign key (business_id) references modern_businesses (id)
);

insert into modern_businesses (id, name, email, password_hash_placeholder, logo_path)
values (
    '11111111-1111-1111-1111-111111111111',
    'Demo Coffee',
    'demo@digitalcards.test',
    'business123',
    '/img/demo-coffee.svg'
)
on duplicate key update
    name = values(name),
    password_hash_placeholder = values(password_hash_placeholder),
    logo_path = values(logo_path);
