CREATE TABLE [Star].[Dim_Date]
(
	[DateKey] INT NOT NULL PRIMARY KEY,
	[Date] DATETIME NOT NULL,
	[DayOfMonth] VARCHAR(2) NOT NULL, -- Field will hold day number of Month
	[DaySuffix] VARCHAR(4) NOT NULL, -- Apply suffix as 1st, 2nd ,3rd etc
	[DayName] VARCHAR(9) NOT NULL, -- Contains name of the day, Sunday, Monday 
	[DayOfWeek] CHAR(1) NOT NULL, -- First Day Monday=1 and Sunday=7
	[DayOfWeekInMonth] VARCHAR(2) NOT NULL, -- 1st Monday or 2nd Monday in Month
	[DayOfWeekInYear] VARCHAR(2) NOT NULL,
	[DayOfQuarter] VARCHAR(3) NOT NULL,
	[DayOfYear] VARCHAR(3) NOT NULL,
	[WeekOfMonth] VARCHAR(1) NOT NULL, -- Week Number of Month 
	[WeekOfQuarter] VARCHAR(2) NOT NULL, -- Week Number of the Quarter
	[WeekOfYear] VARCHAR(2) NOT NULL, -- Week Number of the Year
	[Month] VARCHAR(2) NOT NULL, -- Number of the Month 1 to 12
	[MonthName] VARCHAR(9) NOT NULL, -- January, February etc
	[MonthOfQuarter] VARCHAR(2) NOT NULL, -- Month Number belongs to Quarter
	[Quarter] CHAR(1) NOT NULL,
	[QuarterName] VARCHAR(9) NOT NULL, -- First, Second..
	[Year] CHAR(4) NOT NULL, -- Year value of Date stored in Row
	[MonthYear] CHAR(10) NOT NULL, -- Jan-2013, Feb-2013
	[MMYYYY] CHAR(6) NOT NULL,
	[FirstDayOfMonth] DATE NOT NULL,
	[LastDayOfMonth] DATE NOT NULL,
	[FirstDayOfQuarter] DATE NOT NULL,
	[LastDayOfQuarter] DATE NOT NULL,
	[FirstDayOfYear] DATE NOT NULL,
	[LastDayOfYear] DATE NOT NULL,
	[IsWeekday] BIT NOT NULL,-- 0=Week End ,1=Week Day
)
