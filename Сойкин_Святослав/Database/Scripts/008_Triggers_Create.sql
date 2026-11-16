/**** Почтовое отделение: триггеры ****/
/*
 * Последняя линия защиты: процедуры проверяют то же самое заранее и сообщают причину,
 * триггеры ловят правку в обход процедур (например, из SSMS) и одновременную работу
 * нескольких операторов. Приложение получает SqlException: первой - системную ошибку 3609
 * «транзакция завершилась в триггере», второй - наше сообщение (Number 50000).
 * Запускать после заполнения: скрипт 005 удаляет триггеры перед очисткой таблиц.
 */
use PostOffice_Soykin_PE521;
go

print '*** Создание триггеров ***';
go


-- Таблица УЧАСТКИ -------------------------------------

-- новый участок закрепляется только за работающим почтальоном, не больше предела участков
create or alter trigger onInsertDistricts on Districts
    for insert
as
begin
    declare @count int, @max int = dbo.MaxDistrictsPerPostman();

    -- участки, закреплённые не за работающим почтальоном
    select
        @count = count(*)
    from
        inserted
    where
        inserted.PostmanId not in (select ViewWorkingPostmen.Id from ViewWorkingPostmen);

    if (@count > 0) begin
        raiserror('Districts: участок закрепляется только за работающим почтальоном, участков: %d', 0, 1, @count);
        rollback tran;
        return;
    end;

    -- почтальоны, у которых участков больше предела
    select
        @count = count(*)
    from
        ViewWorkingPostmen
    where
        ViewWorkingPostmen.DistrictCount > @max;

    if (@count > 0) begin
        raiserror('Districts: у почтальона не может быть больше %d участков', 0, 1, @max);
        rollback tran;
    end;
end;
go


-- участок передаётся только работающему почтальону, не больше предела участков
create or alter trigger onUpdateDistricts on Districts
    for update
as
begin
    declare @count int, @max int = dbo.MaxDistrictsPerPostman();

    select
        @count = count(*)
    from
        inserted
    where
        inserted.PostmanId not in (select ViewWorkingPostmen.Id from ViewWorkingPostmen);

    if (@count > 0) begin
        raiserror('Districts: участок передаётся только работающему почтальону, участков: %d', 0, 1, @count);
        rollback tran;
        return;
    end;

    select
        @count = count(*)
    from
        ViewWorkingPostmen
    where
        ViewWorkingPostmen.DistrictCount > @max;

    if (@count > 0) begin
        raiserror('Districts: у почтальона не может быть больше %d участков', 0, 1, @max);
        rollback tran;
    end;
end;
go

-- Таблица УЧАСТКИ -------------------------------------


-- Таблица РАБОТНИКИ -------------------------------------

-- почтальон с участками должен оставаться работающим почтальоном:
-- нельзя уволить или сменить роль, пока за ним закреплены участки
create or alter trigger onUpdateEmployees on Employees
    for update
as
begin
    declare @count int;

    select
        @count = count(*)
    from
        Districts
    where
        Districts.PostmanId not in (select ViewWorkingPostmen.Id from ViewWorkingPostmen);

    if (@count > 0) begin
        raiserror('Employees: нельзя уволить почтальона или сменить ему роль, пока за ним участки: %d', 0, 1, @count);
        rollback tran;
    end;
end;
go

-- Таблица РАБОТНИКИ -------------------------------------


-- Таблица ПОДПИСКИ -------------------------------------

-- новая подписка не пересекается по датам с другой подпиской того же подписчика
-- на то же издание; оформляет работающий оператор или заведующий
create or alter trigger onInsertSubscriptions on Subscriptions
    for insert
as
begin
    declare @count int;

    select
        @count = count(*)
    from
        inserted join Subscriptions on inserted.SubscriberId = Subscriptions.SubscriberId
            and inserted.PublicationId = Subscriptions.PublicationId
            and inserted.Id <> Subscriptions.Id
    where
        inserted.DateStart between Subscriptions.DateStart
            and dbo.SubscriptionEndDate(Subscriptions.DateStart, Subscriptions.Duration)
        or Subscriptions.DateStart between inserted.DateStart
            and dbo.SubscriptionEndDate(inserted.DateStart, inserted.Duration);

    if (@count > 0) begin
        raiserror('Subscriptions: подписка пересекается с другой подпиской на то же издание: %d', 0, 1, @count);
        rollback tran;
        return;
    end;

    select
        @count = count(*)
    from
        inserted
    where
        inserted.OperatorId not in (
            select ViewWorkingEmployees.Id from ViewWorkingEmployees
            where ViewWorkingEmployees.[Role] in ('Оператор', 'Заведующий')
        );

    if (@count > 0) begin
        raiserror('Subscriptions: подписку оформляет работающий оператор или заведующий, записей: %d', 0, 1, @count);
        rollback tran;
    end;
end;
go


-- выданную квитанцию нельзя изменить
create or alter trigger onUpdateSubscriptions on Subscriptions
    for update
as
begin
    declare @count int;

    select
        @count = count(*)
    from
        inserted;

    if (@count > 0) begin
        raiserror('Subscriptions: выданную квитанцию нельзя изменить, записей: %d', 0, 1, @count);
        rollback tran;
    end;
end;
go


-- выданную квитанцию нельзя удалить
create or alter trigger onDeleteSubscriptions on Subscriptions
    for delete
as
begin
    declare @count int;

    select
        @count = count(*)
    from
        deleted;

    if (@count > 0) begin
        raiserror('Subscriptions: выданную квитанцию нельзя удалить, записей: %d', 0, 1, @count);
        rollback tran;
    end;
end;
go

-- Таблица ПОДПИСКИ -------------------------------------

print '*** Завершено создание триггеров ***';
go
