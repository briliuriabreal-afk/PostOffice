/**** Почтовое отделение: резервная копия базы данных ****/
/*
 * Копия создаётся в C:\Temp (служба SQL Server должна иметь право записи в папку),
 * затем файл копируется в папку Database\Backup решения.
 * Перед защитой: перезапустить 005 и 008, затем этот скрипт - свежая копия.
 * Скрипт выполняется в контексте нашей базы: в контексте master при латинской
 * collation сервера кириллица в print превращается в вопросительные знаки.
 */
use PostOffice_Soykin_PE521;
go

print '*** Создание резервной копии базы данных ***';
go

-- удаление логического устройства, если оно осталось от прошлого запуска
if exists (select * from sys.backup_devices where [name] = 'PostOffice_Soykin_PE521_backup')
    exec sp_dropdevice 'PostOffice_Soykin_PE521_backup';
go

-- логическое устройство для резервной копии
exec sp_addumpdevice 'disk', 'PostOffice_Soykin_PE521_backup', 'C:\Temp\PostOffice_Soykin_PE521.bak';
go

-- полная резервная копия; init - перезапись содержимого устройства при повторном сохранении
backup database PostOffice_Soykin_PE521 to PostOffice_Soykin_PE521_backup with init;
go

-- проверка целостности резервной копии
restore verifyonly from PostOffice_Soykin_PE521_backup;
go

print '*** Резервная копия базы данных создана: C:\Temp\PostOffice_Soykin_PE521.bak ***';
go
