# Dynamic OData Query Library

#### This sample has two main purposes
 * Query library integration for simple dynamic OData models. Query Library Integration is as part of code-base and is better understood by going through the sample project.
 * Sample OData queries



#### Sample OData queries:

##### Users
* Metadata
	* Get the metdata of the model exposed by the service<br>
	<https://localhost:44312/odata/entities/user/$metadata>

* Filters<br>
	* Get a list of all users<br>
	<https://localhost:44312/odata/entities/user>

	* Get a list of all users above the age of 30<br>
	[https://localhost:44312/odata/entities/user?$filter=(Age gt 30)](<https://localhost:44312/odata/entities/user?$filter=(Age%20gt%2030)>)

	* Get a list of all users who have Software Engineer in their title<br>
	[https://localhost:44312/odata/entities/user?$filter=contains(Title,'Software Engineer')](<https://localhost:44312/odata/entities/user?$filter=contains(Title,'Software%20Engineer')>)

	* Get a list of all users who have Software Engineer in their title and have salary greater than 200,000<br>
	[https://localhost:44312/odata/entities/user?$filter=contains(Title,'Software Engineer') and Salary gt 200000](<https://localhost:44312/odata/entities/user?$filter=contains(Title,'Software%20Engineer')%20and%20Salary%20gt%20200000>)

* Select<br>
	* Select Title and Salary from users<br>
	<https://localhost:44312/odata/entities/user?$select=id,Title,Salary>

* Apply<br>
	* Group by title, and find the max salary of that group<br>
	[https://localhost:44312/odata/entities/user?$apply=groupby((Title),aggregate(Salary with max as MaxSalary))](<https://localhost:44312/odata/entities/user?$apply=groupby((Title),aggregate(Salary%20with%20max%20as%20MaxSalary))>)

	* Group by title, and find the Total salary of that group<br>
	[https://localhost:44312/odata/entities/user?$apply=groupby((Title),aggregate(Salary with sum as TotalSalary))](<https://localhost:44312/odata/entities/user?$apply=groupby((Title),aggregate(Salary%20with%20sum%20as%20TotalSalary))>)

	* Group by title and list members of that group<br>
	[https://localhost:44312/odata/entities/user?$apply=groupby((Title),aggregate(id with Custom.List as Employees))](<https://localhost:44312/odata/entities/user?$apply=groupby((Title),aggregate(id%20with%20Custom.List%20as%20Employees))>)


* Sorting<br>
	* Get a list of all users ordered by FirstName<br>
	<https://localhost:44312/odata/entities/user?$orderby=FirstName>


* Pagination<br>
	* Gets a list of top 5 users<br>
	<https://localhost:44312/odata/entities/user?$top=5>

	* Gets a list of users skipping the first 5 users<br>
	<https://localhost:44312/odata/entities/user?$skip=5>

	* Pagination using a combination of top & Skip
	<br>Page 1: <https://localhost:44312/odata/entities/user?$top=5> 
	<br>Page 2: <https://localhost:44312/odata/entities/user?$top=5&$skip=5>


##### Products
* Metadata
	* Get the metdata of the model exposed by the service<br>
	<https://localhost:44312/odata/entities/product/$metadata>
	
* Filters
	* Get a list of all products<br>
  	<https://localhost:44312/odata/entities/product>

	* Get a list of all products with Azure in its name<br>
  	[https://localhost:44312/odata/entities/product?$filter=contains(Name,'Azure')](<https://localhost:44312/odata/entities/product?$filter=contains(Name,'Azure')>)

* Select<br>
	* Select Name and Description from products<br>
	<https://localhost:44312/odata/entities/product?$select=Name,Description>

* Apply<br>
	* Group products by BrandName and list them<br>
	[https://localhost:44312/odata/entities/product?$apply=groupby((BrandName),aggregate(id with Custom.List as Products))](<https://localhost:44312/odata/entities/product?$apply=groupby((BrandName),aggregate(id%20with%20Custom.List%20as%20Products))>)

* Sorting<br>
	* Get a list of all products ordered by BrandName ascending, then by Name descending<br>
	[https://localhost:44312/odata/entities/product?$orderby=BrandName asc,Name desc](<https://localhost:44312/odata/entities/product?$orderby=BrandName%20asc,Name%20desc>)

<br>
######Copyright (c) Microsoft Corporation.  All rights reserved.
######Licensed under the MIT License.  See License.txt in the project root for license information.
