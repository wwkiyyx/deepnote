create table fileTmpTest(
	file_datetime char(50),
	ip_port char(50),
	file_name char(200)
);

create table srcFile(
	file_datetime char(50),
	ip_port char(50),
	file_name char(200)
);

create table resFile(
	file_datetime char(50),
	file_name char(200),
	lot char(50),
	dt char(50),
	d char(50),
	res_dir char(255),
	res char(20),
	res_ch char(50),
	x char(10)
);

create table fileTmpTest2(
	file_datetime char(50),
	ip_port char(50),
	file_name char(200)
);

create table resFile2(
	file_datetime char(50),
	file_name char(200),
	lot char(50),
	dt char(50),
	d char(50),
	res_dir1 char(255),
	res_dir3 char(255),
	res1 int,
	res2 int,
	res3 int
);