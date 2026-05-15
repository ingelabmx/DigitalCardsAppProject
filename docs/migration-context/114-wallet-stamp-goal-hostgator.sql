alter table ModernBusinessBranding
    add column StampGoal int not null default 10 after CustomFieldColor;

update ModernBusinessBranding
set StampGoal = 10
where StampGoal is null or StampGoal <= 0;
