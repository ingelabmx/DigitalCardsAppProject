-- Admin Business Activation Status
-- Ejecutar manualmente en HostGator antes de usar estados de activacion por negocio.
-- Extiende ModernPilotBusiness sin tocar tablas legacy ni Web Forms.

set @activation_status_column_exists := (
    select count(*)
    from information_schema.columns
    where table_schema = database()
      and table_name = 'ModernPilotBusiness'
      and column_name = 'ActivationStatus'
);

set @activation_status_sql := if(
    @activation_status_column_exists = 0,
    'alter table ModernPilotBusiness add column ActivationStatus varchar(32) not null default ''LegacyOnly'' after IsEnabled',
    'select ''ModernPilotBusiness.ActivationStatus already exists'''
);

prepare activation_status_stmt from @activation_status_sql;
execute activation_status_stmt;
deallocate prepare activation_status_stmt;

update ModernPilotBusiness
set ActivationStatus = 'PilotModern'
where IsEnabled = 1
  and ActivationStatus = 'LegacyOnly';

set @activation_status_index_exists := (
    select count(*)
    from information_schema.statistics
    where table_schema = database()
      and table_name = 'ModernPilotBusiness'
      and index_name = 'IX_ModernPilotBusiness_ActivationStatus'
);

set @activation_status_index_sql := if(
    @activation_status_index_exists = 0,
    'alter table ModernPilotBusiness add key IX_ModernPilotBusiness_ActivationStatus (ActivationStatus)',
    'select ''IX_ModernPilotBusiness_ActivationStatus already exists'''
);

prepare activation_status_index_stmt from @activation_status_index_sql;
execute activation_status_index_stmt;
deallocate prepare activation_status_index_stmt;

