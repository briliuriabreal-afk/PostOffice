/**** Почтовое отделение: заполнение таблиц ****/
/*
 * Даты фиксированные, окно стабильности - с 01.10.2026 по 31.12.2026: при любом «сегодня»
 * из окна запросы «на текущий момент» дают одни и те же ответы. В окне ни одна подписка
 * не начинается в ноябре-декабре 2026 и не заканчивается в октябре-ноябре 2026.
 *
 * Классы подписок (оформлена / начало / срок):
 *   A - 10.09.2026 / 01.10.2026 / 3 или 6 мес.; 15.06.2026 / 01.07.2026 / 12 мес. - действуют сейчас
 *   B - 10.12.2025 / 01.01.2026 / 12 мес. - действуют сейчас, оформлены в прошлом году
 *   C - 15.10.2026 / 01.01.2027 / 6 мес. - ещё не начались
 *   D - январь 2026 / 01.02.2026 / 3 или 6 мес. - завершились в этом году
 *   E - 10.03.2025 / 01.04.2025 / 6 мес. - завершились в прошлом году
 *   F - 05.11.2024 / 01.12.2024 / 3 мес. - позапрошлый год
 *
 * Учётные записи для проверки, пароль у всех - post2026:
 *   belova - заведующий; kozlova, morozov - операторы;
 *   petrov, smirnova, vasiliev - почтальоны; nikolaev - почтальон и подписчик;
 *   fedorova - уволенный почтальон, подписчик;
 *   kuznetsov, li, ivanov, sokolova, orlova, lebedev, novikova, popov, volkova, golubeva - подписчики
 */
use PostOffice_Soykin_PE521;
go

-- удаление триггеров - важно при повторном выполнении скрипта:
-- триггер отмены удаления не дал бы очистить таблицу подписок;
-- триггеры заново создаёт скрипт 008_Triggers_Create
drop trigger if exists onInsertDistricts;
drop trigger if exists onUpdateDistricts;
drop trigger if exists onUpdateEmployees;
drop trigger if exists onInsertSubscriptions;
drop trigger if exists onUpdateSubscriptions;
drop trigger if exists onDeleteSubscriptions;
go

begin tran
print '*** Заполнение таблиц данными ***';


-- удаление всех записей - важно при повторном выполнении скрипта
delete from Subscriptions;
delete from Publications;
delete from PublicationTypes;
delete from Accounts;
delete from Subscribers;
delete from Houses;
delete from Districts;
delete from Employees;
delete from Roles;
delete from People;
delete from Streets;


/**** улицы - Streets ****/
set identity_insert Streets on;
insert into Streets (Id, [Name]) values
    (1, 'ул. Ленина'),
    (2, 'пр. Мира'),
    (3, 'ул. Садовая'),
    (4, 'ул. Гагарина'),          -- дома есть, подписчиков нет
    (5, 'ул. Советская'),
    (6, 'пер. Почтовый');         -- частные дома
declare @errStreets int = @@error;
set identity_insert Streets off;
save tran StreetsTP;


/**** роли работников - Roles ****/
set identity_insert Roles on;
insert into Roles (Id, [Name]) values
    (1, 'Заведующий'),
    (2, 'Оператор'),
    (3, 'Почтальон');
declare @errRoles int = @@error;
set identity_insert Roles off;
save tran RolesTP;


/**** виды изданий - PublicationTypes ****/
set identity_insert PublicationTypes on;
insert into PublicationTypes (Id, [Name]) values
    (1, 'Газета'),
    (2, 'Журнал');
declare @errPublicationTypes int = @@error;
set identity_insert PublicationTypes off;
save tran PublicationTypesTP;


/**** люди - People ****/
set identity_insert People on;
insert into People (Id, Surname, [Name], Patronymic, Passport) values
    ( 1, 'Белова',     'Ирина',    'Сергеевна',   '4501 112233'),   -- заведующий
    ( 2, 'Козлова',    'Анна',     'Викторовна',  '4502 223344'),   -- оператор
    ( 3, 'Морозов',    'Дмитрий',  'Андреевич',   '4503 334455'),   -- оператор
    ( 4, 'Петров',     'Иван',     'Николаевич',  '4504 445566'),   -- почтальон, 5 участков
    ( 5, 'Смирнова',   'Ольга',    'Петровна',    '4505 556677'),   -- почтальон, 2 участка
    ( 6, 'Васильев',   'Сергей',   'Олегович',    '4506 667788'),   -- почтальон, 2 участка
    ( 7, 'Николаев',   'Алексей',  'Игоревич',    '4507 778899'),   -- почтальон и подписчик
    ( 8, 'Фёдорова',   'Марина',   'Юрьевна',     '4508 889900'),   -- уволенный почтальон, подписчик
    ( 9, 'Кузнецов',   'Андрей',   'Павлович',    '4510 100201'),
    (10, 'Ли',         'Виктор',   null,          '4510 100202'),   -- без отчества
    (11, 'Иванов',     'Иван',     'Иванович',    '4510 100203'),   -- полный тёзка 12
    (12, 'Иванов',     'Иван',     'Иванович',    '4510 100204'),   -- полный тёзка 11
    (13, 'Соколова',   'Елена',    'Михайловна',  '4510 100205'),   -- одна квартира с 14
    (14, 'Соколов',    'Павел',    'Андреевич',   '4510 100206'),
    (15, 'Орлова',     'Татьяна',  'Викторовна',  '4510 100207'),   -- подписчик без подписок
    (16, 'Лебедев',    'Николай',  'Степанович',  '4510 100208'),   -- частный дом
    (17, 'Новикова',   'Светлана', 'Игоревна',    '4510 100209'),
    (18, 'Попов',      'Артём',    'Денисович',   '4510 100210'),
    (19, 'Волкова',    'Наталья',  'Алексеевна',  '4510 100211'),
    (20, 'Зайцев',     'Михаил',   'Романович',   '4510 100212'),
    (21, 'Павлова',    'Дарья',    'Сергеевна',   '4510 100213'),   -- продление подписки
    (22, 'Семёнов',    'Григорий', 'Ильич',       '4510 100214'),
    (23, 'Голубева',   'Вера',     'Николаевна',  '4510 100215'),
    (24, 'Виноградов', 'Олег',     'Петрович',    '4510 100216'),
    (25, 'Богданова',  'Ксения',   'Андреевна',   '4510 100217'),   -- частный дом
    (26, 'Тарасов',    'Юрий',     'Витальевич',  '4510 100218'),
    (27, 'Ершова',     'Алла',     'Геннадьевна', '4510 100219');   -- без учётной записи
declare @errPeople int = @@error;
set identity_insert People off;
save tran PeopleTP;


/**** работники - Employees ****/
set identity_insert Employees on;
insert into Employees (Id, PersonId, RoleId, HireDate, FireDate) values
    (1, 1, 1, '2019-03-01', null),
    (2, 2, 2, '2021-09-01', null),
    (3, 3, 2, '2023-02-15', null),
    (4, 4, 3, '2018-05-14', null),
    (5, 5, 3, '2020-06-01', null),
    (6, 6, 3, '2022-04-11', null),
    (7, 7, 3, '2024-09-02', null),
    (8, 8, 3, '2021-01-11', '2026-06-30');   -- уволена
declare @errEmployees int = @@error;
set identity_insert Employees off;
save tran EmployeesTP;


/**** участки - Districts ****/
set identity_insert Districts on;
insert into Districts (Id, Number, PostmanId) values
    ( 1,  1, 4),        -- Петров: участки 1-5, предел
    ( 2,  2, 4),
    ( 3,  3, 4),
    ( 4,  4, 4),
    ( 5,  5, 4),        -- без подписчиков
    ( 6,  6, 5),        -- Смирнова
    ( 7,  7, 5),
    ( 8,  8, 6),        -- Васильев
    ( 9,  9, 6),
    (10, 10, 7);        -- Николаев
declare @errDistricts int = @@error;
set identity_insert Districts off;
save tran DistrictsTP;


/**** дома - Houses ****/
set identity_insert Houses on;
insert into Houses (Id, StreetId, Building, DistrictId) values
    ( 1, 1, '1',     1),    -- ул. Ленина делится между участками 1 и 2
    ( 2, 1, '3',     1),
    ( 3, 1, '5',     1),
    ( 4, 1, '10',    2),
    ( 5, 1, '12',    2),
    ( 6, 1, '14а',   2),
    ( 7, 2, '2',     3),
    ( 8, 2, '4',     3),
    ( 9, 2, '6/1',   3),
    (10, 2, '15',    4),
    (11, 2, '17',    4),
    (12, 4, '1',     5),
    (13, 4, '3',     5),
    (14, 3, '7',     6),
    (15, 3, '9',     6),
    (16, 3, '11',    6),
    (17, 3, '20',    7),
    (18, 3, '22к1',  7),
    (19, 5, '5',     8),
    (20, 5, '8',     8),
    (21, 5, '10',    8),
    (22, 5, '31',    9),
    (23, 5, '33',    9),
    (24, 6, '2',    10),
    (25, 6, '4',    10),
    (26, 6, '6',    10);
declare @errHouses int = @@error;
set identity_insert Houses off;
save tran HousesTP;


/**** подписчики - Subscribers ****/
set identity_insert Subscribers on;
insert into Subscribers (Id, PersonId, HouseId, Flat) values
    ( 1,  9,  5,    7),     -- Кузнецов, участок 2
    ( 2, 10,  8,    3),     -- Ли, участок 3
    ( 3, 11,  4,   15),     -- Иванов, участок 2
    ( 4, 12, 15,    2),     -- Иванов, участок 6
    ( 5, 13,  8,   12),     -- Соколова, участок 3
    ( 6, 14,  8,   12),     -- Соколов, та же квартира
    ( 7, 15,  1,    4),     -- Орлова, участок 1
    ( 8, 16, 24, null),     -- Лебедев, участок 10, частный дом
    ( 9, 17,  2,   21),     -- Новикова, участок 1
    (10, 18,  7,    9),     -- Попов, участок 3
    (11, 19, 10,   30),     -- Волкова, участок 4
    (12, 20, 14,   11),     -- Зайцев, участок 6
    (13, 21, 17,    5),     -- Павлова, участок 7
    (14, 22, 19,    8),     -- Семёнов, участок 8
    (15, 23, 22,   40),     -- Голубева, участок 9
    (16, 24, 20,    2),     -- Виноградов, участок 8
    (17, 25, 25, null),     -- Богданова, участок 10, частный дом
    (18, 26, 11,    6),     -- Тарасов, участок 4
    (19, 27, 18,   14),     -- Ершова, участок 7
    (20,  7, 26,    1),     -- Николаев, участок 10
    (21,  8, 16,    3);     -- Фёдорова, участок 6
declare @errSubscribers int = @@error;
set identity_insert Subscribers off;
save tran SubscribersTP;


/**** учётные записи - Accounts ****/
set identity_insert Accounts on;
insert into Accounts (Id, PersonId, [Login], PasswordHash) values
    ( 1,  1, 'belova',    dbo.PasswordHash('belova',    'post2026')),
    ( 2,  2, 'kozlova',   dbo.PasswordHash('kozlova',   'post2026')),
    ( 3,  3, 'morozov',   dbo.PasswordHash('morozov',   'post2026')),
    ( 4,  4, 'petrov',    dbo.PasswordHash('petrov',    'post2026')),
    ( 5,  5, 'smirnova',  dbo.PasswordHash('smirnova',  'post2026')),
    ( 6,  6, 'vasiliev',  dbo.PasswordHash('vasiliev',  'post2026')),
    ( 7,  7, 'nikolaev',  dbo.PasswordHash('nikolaev',  'post2026')),
    ( 8,  8, 'fedorova',  dbo.PasswordHash('fedorova',  'post2026')),
    ( 9,  9, 'kuznetsov', dbo.PasswordHash('kuznetsov', 'post2026')),
    (10, 10, 'li',        dbo.PasswordHash('li',        'post2026')),
    (11, 11, 'ivanov',    dbo.PasswordHash('ivanov',    'post2026')),
    (12, 13, 'sokolova',  dbo.PasswordHash('sokolova',  'post2026')),
    (13, 15, 'orlova',    dbo.PasswordHash('orlova',    'post2026')),
    (14, 16, 'lebedev',   dbo.PasswordHash('lebedev',   'post2026')),
    (15, 17, 'novikova',  dbo.PasswordHash('novikova',  'post2026')),
    (16, 18, 'popov',     dbo.PasswordHash('popov',     'post2026')),
    (17, 19, 'volkova',   dbo.PasswordHash('volkova',   'post2026')),
    (18, 23, 'golubeva',  dbo.PasswordHash('golubeva',  'post2026'));
declare @errAccounts int = @@error;
set identity_insert Accounts off;
save tran AccountsTP;


/**** подписные издания - Publications ****/
set identity_insert Publications on;
insert into Publications (Id, PubIndex, Title, PublicationTypeId, Price) values
    ( 1, '50050', 'Комсомольская правда', 1, 120),   -- была 110: старые квитанции со старой ценой
    ( 2, '50103', 'Аргументы и факты',    1, 98.5),
    ( 3, '50126', 'Российская газета',    1, 140),
    ( 4, 'П2011', 'Вечерний город',       1, 65),
    ( 5, '32035', 'Спорт-Экспресс',       1, 110),
    ( 6, '50190', 'Труд',                 1, 75),
    ( 7, 'П3055', 'Сельская новь',        1, 45),    -- никто не выписывал
    ( 8, '70630', 'Наука и жизнь',        2, 390),
    ( 9, '71203', 'Вокруг света',         2, 450),   -- сроки 3, 6 и 12: средний 7
    (10, '70141', 'Огонёк',               2, 280),   -- сроки 3 и 6: средний 4,5
    (11, 'Е1304', 'Здоровье',             2, 210),
    (12, '73022', 'Мурзилка',             2, 260);   -- только завершённые подписки
declare @errPublications int = @@error;
set identity_insert Publications off;
save tran PublicationsTP;


/**** подписки - Subscriptions ****/
set identity_insert Subscriptions on;
insert into Subscriptions
    (Id, SubscriberId, OperatorId, PublicationId, IssueDate, DateStart, Duration, Copies, Price)
values
    ( 1,  1, 2,  1, '2026-09-10', '2026-10-01',  6, 1, 120),    -- A
    ( 2,  1, 3,  8, '2025-12-10', '2026-01-01', 12, 1, 390),    -- B
    ( 3,  1, 2,  6, '2026-01-15', '2026-02-01',  6, 1, 75),     -- D
    ( 4,  2, 2,  2, '2025-12-10', '2026-01-01', 12, 1, 98.5),   -- B
    ( 5,  2, 3, 10, '2026-01-20', '2026-02-01',  3, 1, 280),    -- D
    ( 6,  3, 3,  3, '2026-06-15', '2026-07-01', 12, 1, 140),    -- A
    ( 7,  3, 2,  5, '2025-03-10', '2025-04-01',  6, 1, 110),    -- E
    ( 8,  4, 2,  1, '2025-12-10', '2026-01-01', 12, 1, 110),    -- B, старая цена
    ( 9,  4, 3,  9, '2026-10-15', '2027-01-01',  6, 1, 450),    -- C
    (10,  5, 2,  1, '2026-09-10', '2026-10-01',  3, 1, 120),    -- A
    (11,  5, 2, 11, '2026-09-10', '2026-10-01',  6, 1, 210),    -- A
    (12,  6, 3,  1, '2026-09-10', '2026-10-01',  3, 1, 120),    -- A, тот же адрес
    (13,  8, 2,  4, '2025-12-10', '2026-01-01', 12, 2, 65),     -- B, 2 экземпляра
    (14,  8, 2, 12, '2024-11-05', '2024-12-01',  3, 1, 240),    -- F
    (15,  9, 3,  2, '2026-09-10', '2026-10-01',  6, 1, 98.5),   -- A
    (16,  9, 2, 12, '2025-03-10', '2025-04-01',  6, 1, 250),    -- E
    (17, 10, 3,  5, '2025-12-10', '2026-01-01', 12, 1, 110),    -- B
    (18, 10, 2, 10, '2026-09-10', '2026-10-01',  6, 1, 280),    -- A
    (19, 11, 3,  9, '2026-01-20', '2026-02-01',  3, 1, 450),    -- D
    (20, 11, 2,  3, '2026-10-15', '2027-01-01',  6, 1, 140),    -- C
    (21, 12, 3,  9, '2026-06-15', '2026-07-01', 12, 1, 450),    -- A
    (22, 12, 2,  6, '2025-12-10', '2026-01-01', 12, 2, 75),     -- B, 2 экземпляра
    (23, 13, 1, 11, '2025-12-10', '2026-01-01', 12, 1, 210),    -- B, оформил заведующий
    (24, 13, 2,  1, '2026-01-15', '2026-02-01',  6, 1, 110),    -- D, старая цена
    (25, 13, 3,  1, '2026-09-10', '2026-10-01',  6, 1, 120),    -- A, продление 24
    (26, 14, 3,  5, '2026-06-15', '2026-07-01', 12, 2, 110),    -- A, 2 экземпляра
    (27, 14, 2,  8, '2026-10-15', '2027-01-01',  6, 1, 390),    -- C
    (28, 15, 3, 11, '2026-01-15', '2026-02-01',  6, 1, 210),    -- D
    (29, 15, 2,  2, '2026-09-10', '2026-10-01',  6, 1, 98.5),   -- A
    (30, 16, 2,  3, '2025-12-10', '2026-01-01', 12, 1, 140),    -- B
    (31, 16, 3,  4, '2026-10-15', '2027-01-01',  6, 1, 65),     -- C
    (32, 17, 3,  1, '2025-12-10', '2026-01-01', 12, 1, 110),    -- B, старая цена
    (33, 17, 2,  8, '2025-03-10', '2025-04-01',  6, 1, 370),    -- E
    (34, 18, 3,  6, '2026-09-10', '2026-10-01',  6, 1, 75),     -- A
    (35, 19, 2,  4, '2026-06-15', '2026-07-01', 12, 1, 65),     -- A
    (36, 20, 3,  5, '2026-09-10', '2026-10-01',  6, 3, 110),    -- A, 3 экземпляра
    (37, 21, 2,  2, '2026-01-15', '2026-02-01',  6, 1, 98.5),   -- D
    (38, 21, 3, 11, '2026-06-15', '2026-07-01', 12, 1, 210);    -- A
declare @errSubscriptions int = @@error;
set identity_insert Subscriptions off;
save tran SubscriptionsTP;


-- фиксация, если ошибок не было, иначе полный откат
if (@errStreets = 0 and @errRoles = 0 and @errPublicationTypes = 0 and @errPeople = 0
    and @errEmployees = 0 and @errDistricts = 0 and @errHouses = 0 and @errSubscribers = 0
    and @errAccounts = 0 and @errPublications = 0 and @errSubscriptions = 0) begin
    commit tran;
    print '*** Заполнение таблиц завершено ***';
end else begin
    rollback tran;
    print '*** Ошибка заполнения таблиц, изменения отменены ***';
end;
go
