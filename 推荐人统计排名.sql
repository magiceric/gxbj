#SELECT * FROM gxbj.message_rawdata;
#SELECT * FROM gxbj.message_used;
#SELECT * FROM gxbj.message_abandoned;

#SELECT * FROM employeebase WHERE deptname LIKE '%投资研究部%';
#SELECT * FROM employeebase WHERE deptname LIKE '%投融资部%';
#SELECT * FROM employeebase WHERE emplname LIKE '%李芳辉%';

UPDATE employeebase
SET deptid='60000052', deptname='投资研发中心', directorid='XS019', directorname='刘宇'
WHERE deptid IN ('60000053');

UPDATE employeebase
SET deptid='60000054', deptname='投资研究部', directorid='CF023', directorname='李芳辉'
WHERE deptid IN ('60000054');

UPDATE employeebase
SET deptid='60000041', deptname='运营服务中心', directorid='JY010', directorname='谢永丹'
WHERE deptid IN ('60000042','60000044','60000045','60000046','60000047');

UPDATE employeebase
SET deptid='60000039', deptname='业务管理中心', directorid='F1443', directorname='方磊'
WHERE deptid IN ('60000002','60000011','60000040','60000046','60000056','60000057','60000062');

UPDATE employeebase
SET deptid='60000051', deptname='总经理办公室', directorid='SC014', directorname='肖玮'
WHERE deptid IN ('60000064','60000069');

SELECT proposer AS notes号, emplname AS 姓名, COUNT(*) AS 目前推荐数
FROM gxbj.message_used used
LEFT JOIN employeebase empl ON empl.notesid=used.proposer
WHERE used.checktime<='20140930'
GROUP BY proposer, empl.emplname
ORDER BY COUNT(*) DESC;

SELECT proposer AS notes号, empl.emplname AS 姓名, COUNT(*) AS 月推荐数, 
	DATE_FORMAT(DATE_ADD(NOW(), interval -1 day),"%Y-%m-%d") AS 计增日, 
	CASE WHEN incr.daycount IS NOT NULL THEN incr.daycount ELSE 0 END AS 日上升数
FROM gxbj.message_used used
LEFT JOIN employeebase empl ON empl.notesid=used.proposer
LEFT JOIN (
	SELECT proposer AS notesid, emplname, DATE_FORMAT(used.checktime,'%Y%m%d') AS checkdate, COUNT(*) AS daycount
	FROM gxbj.message_used used
	LEFT JOIN employeebase empl ON empl.notesid=used.proposer
	WHERE ( used.checktime>='20141001' AND  used.checktime<'20141101')
	GROUP BY proposer, empl.emplname, DATE_FORMAT(used.checktime,'%Y%m%d')
	ORDER BY proposer, DATE_FORMAT(used.checktime,'%Y%m%d')
) incr ON incr.notesid=empl.notesid AND incr.checkdate=DATE_FORMAT(DATE_ADD(NOW(), interval -1 day),'%Y%m%d')
WHERE ( used.checktime>='20141001' AND  used.checktime<'20141101')
GROUP BY proposer, empl.emplname
ORDER BY incr.daycount DESC;

SELECT proposer, empl.emplname, COUNT(*) AS checkcount, 
	DATE_FORMAT(DATE_ADD(NOW(), interval 0 day),'%Y-%m-%d') AS incdate, 
	CASE WHEN incr.daycount IS NOT NULL THEN incr.daycount ELSE 0 END AS incamount 
FROM gxbj.message_used used 
LEFT JOIN employeebase empl ON empl.notesid=used.proposer 
LEFT JOIN ( 
	SELECT proposer AS notesid, emplname, DATE_FORMAT(used.checktime,'%Y%m%d') AS checkdate, COUNT(*) AS daycount 
	FROM gxbj.message_used used 
	LEFT JOIN employeebase empl ON empl.notesid=used.proposer 
	WHERE ( used.checktime>='20141001' AND used.checktime<='20141101') 
	GROUP BY proposer, empl.emplname, DATE_FORMAT(used.checktime,'%Y%m%d') 
	ORDER BY proposer, DATE_FORMAT(used.checktime,'%Y%m%d') 
) incr ON incr.notesid=empl.notesid AND incr.checkdate=DATE_FORMAT(DATE_ADD(NOW(), interval 0 day),'%Y%m%d') 
WHERE ( used.checktime>='20141001' AND used.checktime<'20141101') 
GROUP BY proposer, empl.emplname 
ORDER BY incr.daycount DESC;




SELECT *
FROM gxbj.message_used;

SELECT proposer AS notesid, emplname, DATE_FORMAT(used.checktime,'%Y%m%d') AS checkdate, COUNT(*) AS daycount
FROM gxbj.message_used used
LEFT JOIN employeebase empl ON empl.notesid=used.proposer
WHERE ( used.checktime>='20141001' AND  used.checktime<'20141101')
GROUP BY proposer, empl.emplname, DATE_FORMAT(used.checktime,'%Y%m%d')
ORDER BY proposer, DATE_FORMAT(used.checktime,'%Y%m%d');

SELECT DATE_FORMAT('2014-1-1','%Y%m%d');

SELECT deptname AS 部门, directorname AS 总监, COUNT(*) AS 直辖总推荐数, dept.mancount AS 直辖人数, COUNT(*)/mancount AS 目前人均
FROM gxbj.message_used used
LEFT JOIN employeebase empl ON empl.notesid=used.proposer
LEFT JOIN (
	SELECT deptid, COUNT(*) AS mancount
	FROM gxbj.employeebase
	WHERE notesid IS NOT NULL AND notesid<>''
	GROUP BY deptid	
) dept ON dept.deptid=empl.deptid
GROUP BY directorname, dept.mancount
ORDER BY COUNT(*)/mancount DESC;

UPDATE updatetime
SET lastupdatetime=NOW();

