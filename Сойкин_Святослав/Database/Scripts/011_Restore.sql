/**** Почтовое отделение: восстановление базы данных из резервной копии ****/
/*
 * Восстановление требует монопольного доступа: все подключения к базе
 * (окна запросов SSMS, приложение) должны быть закрыты.
 * restore восстанавливает базу данных поверх существующей.
 * Файл копии - C:\Temp\PostOffice_Soykin_PE521.bak (скопировать туда из Database\Backup).
 * Восстановление идёт из контекста master, поэтому сообщение с кириллицей -
 * только после перехода в восстановленную базу.
 */
use master;
go

restore database PostOffice_Soykin_PE521 from disk = 'C:\Temp\PostOffice_Soykin_PE521.bak';
go

-- контроль: база данных восстановлена вместе с данными
use PostOffice_Soykin_PE521;
go

select
    (select count(*) from Subscribers) as SubscribersCount
    , (select count(*) from Subscriptions) as SubscriptionsCount
    , (select count(*) from Districts) as DistrictsCount;
go

print '*** База данных восстановлена ***';
go
