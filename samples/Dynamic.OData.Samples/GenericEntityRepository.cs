// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Bogus;
using Dynamic.OData.Samples.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Dynamic.OData.Samples
{
    /// <summary>
    /// Mock repository class. In real world you would get it from some store.
    /// </summary>
    public class GenericEntityRepository : IGenericEntityRepository
    {
        private static List<GenericEntity> totalEntityList = new List<GenericEntity>();
        public GenericEntityRepository(int count = 50)
        {
            totalEntityList.Clear();
            totalEntityList.AddRange(GetUserEntities(count));
            totalEntityList.AddRange(GetProductEntities());
        }
        public IEnumerable<GenericEntity> GetEntities(string entityName)
        {
            return totalEntityList.Where(p => string.Equals(p.EntityName, entityName, StringComparison.OrdinalIgnoreCase));
        }


        /// <summary>
        /// Gets Product EntityData. The data below is for representation only and is in no way an indicator of real data.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<GenericEntity> GetProductEntities()
        {
            var products = new List<GenericEntity>();
            products.Add(GetProductEntity(Guid.Parse("2664f337-4a92-4bc4-aabf-a94e818f49a3".ToLower()), "Office 365 E3", 1, "Office 365 Plans", 250, 360, "Office"));
            products.Add(GetProductEntity(Guid.Parse("b998ee4d-3c0a-40e7-aa53-26c15bd878e5".ToLower()), "Windows 10", 2, "Consumer OS", 399, 499, "Windows"));
            products.Add(GetProductEntity(Guid.Parse("52d691a2-f44c-4644-81c4-1d81190c117c".ToLower()), "XBOX One S", 3, "Gaming Device", 200, 499, "XBox"));
            products.Add(GetProductEntity(Guid.Parse("fc3cc48d-02b0-4b58-9d64-f63b2167685a".ToLower()), "Dynamics 365", 4, "Dynamics 365 Business Products", 190, 245, "Dynamics"));
            products.Add(GetProductEntity(Guid.Parse("d7428831-4707-46ab-91e8-f84c50476440".ToLower()), "Azure KeyVault", 5, "Azure KeyVault Service for secrent Management", 50, 100, "Azure"));
            products.Add(GetProductEntity(Guid.Parse("04ac927e-fabf-40db-a2c7-661ee12939cb".ToLower()), "Azure App Service", 5, "Azure App Service for Web Hosting", 500, 900, "Azure"));
            products.Add(GetProductEntity(Guid.Parse("db7ff8e0-2f1c-4455-840e-0eca338dec99".ToLower()), "Azure Functions", 5, "Serverless Azure functions", 80, 170, "Azure"));
            products.Add(GetProductEntity(Guid.Parse("49A4BAAF-9902-4E89-88E5-36CC6A4DB8FE".ToLower()), "Azure Service Bus", 5, "Serverless Azure functions", 80, 170, "Azure"));
            products.Add(GetProductEntity(Guid.Parse("088284f0-dd43-4d51-bbdd-3dd6e964a07a".ToLower()), "Windows Server", 2, "Server OS", 100, 325, "Windows"));
            products.Add(GetProductEntity(Guid.Parse("81740a7c-b00f-45a3-b991-e03132a46712".ToLower()), "XBOX 360", 3, "Legacy Gaming Device", 100, 200, "XBox"));
            products.Add(GetProductEntity(Guid.Parse("937fb1e8-bafc-4824-af5a-33a453ec2cd2".ToLower()), "Office 365 E5", 1, "Office 365", 280, 430, "Office"));
            products.Add(GetProductEntity(Guid.Parse("2a064a38-4c76-4c14-9357-b7fa5e03426d".ToLower()), "Dynamics On Premise", 4, "Dynamics onpremise", 150, 280, "Dynamics"));
            return products;
        }


        /// <summary>
        /// Gets User entity data.This is for representation only and is in no way an indicator of real data.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<GenericEntity> GetUserEntities(int count)
        {
            var users = new List<GenericEntity>();
            Randomizer.Seed = new Random(3897234);
            var titles = new[] { "Project Manager", "Corporate VP", "General Manager", "Software Engineer", "Software Engineer 2", "Senior Software Engineer", "Senior Project Manager"};
            var age = new [] { 22, 28, 35, 50, 55, 44, 33 };
            var salaries = new[] { 200000, 500000, 1000000, 1500000, 150000, 124000 };
            var departments = new[] { "Office", "Windows", "Azure", "Dynamics", "XBox" };
            var vacationDays = new[] { 22.67899, 72.67899, 42.67899, 24.67899, 32.56756, 65.7843 };
            short employeeNumber = 1;
            long universalId = 22335678998;
            var userGenerator = new Faker<User>()
                .CustomInstantiator(f => new User(employeeNumber++, universalId++))
                .RuleFor(p => p.id, q => Guid.NewGuid())
                .RuleFor(p => p.Title, q => q.PickRandom(titles))
                .RuleFor(p => p.FirstName, q => q.Name.FirstName())
                .RuleFor(p => p.LastName, q => q.Name.LastName())
                .RuleFor(p => p.Age, (q, p) => p.Title == "General Manager" || p.Title == "Corporate VP" ?
                    q.PickRandom(new[] { 44, 50, 55 }) : q.PickRandom(age))
                .RuleFor(p => p.Salary, (q, p) => p.Title == "General Manager" || p.Title == "Corporate VP" ?
                    q.PickRandom(new[] { 1500000, 1000000, 500000 }) : q.PickRandom(salaries))
                .RuleFor(p => p.BornOn, (q, p) => DateTime.UtcNow.AddYears(-p.Age))
                .RuleFor(p => p.Department, q => q.PickRandom(departments))
                .RuleFor(p => p.EmailAddress, (q, p) => p.FirstName.ToLower() + "." + p.LastName.ToLower() + "@contoso.com")
                .RuleFor(p => p.VacationDaysInHours, q => q.PickRandom(vacationDays));

            var fakeUsers = userGenerator.Generate(count);
            foreach (var user in fakeUsers)
                users.Add(GetUserEntity(user.id, user.Title, user.FirstName, user.LastName, user.Age, user.Salary, user.BornOn, user.Department, user.EmailAddress, user.EmployeeNumber, user.UniversalId, user.VacationDaysInHours));           
            return users;
        }



        private GenericEntity GetUserEntity(Guid id
        , string title
        , string firstName
        , string lastName
        , int age
        , decimal salary
        , DateTime bornOnDate
        , string department
        , string emailAddress
        , short employeeNumber
        , long universalId
        , double vacationDaysInHours)
        {
            var userEntity = new GenericEntity { EntityName = "User", PropertyList = new Dictionary<string, object>() };
            userEntity.PropertyList.Add("id", id);
            userEntity.PropertyList.Add("Title", title);
            userEntity.PropertyList.Add("FirstName", firstName);
            userEntity.PropertyList.Add("LastName", lastName);
            userEntity.PropertyList.Add("Age", age);
            userEntity.PropertyList.Add("Salary", salary);
            userEntity.PropertyList.Add("BornOn", bornOnDate);
            userEntity.PropertyList.Add("Department", department);
            userEntity.PropertyList.Add("EmailAddress", emailAddress);
            userEntity.PropertyList.Add("EmployeeNumber", employeeNumber);
            userEntity.PropertyList.Add("UniversalId", universalId);
            userEntity.PropertyList.Add("VacationDaysInHours", vacationDaysInHours);
            return userEntity;
        }

        private GenericEntity GetProductEntity(Guid id
       , string name
       , int type
       , string description
       , decimal unitcost
       , decimal unitprice
       , string brandname)
        {
            var userEntity = new GenericEntity { EntityName = "Product", PropertyList = new Dictionary<string, object>() };
            userEntity.PropertyList.Add("id", id);
            userEntity.PropertyList.Add("Name", name);
            userEntity.PropertyList.Add("Type", type);
            userEntity.PropertyList.Add("Description", description);
            userEntity.PropertyList.Add("UnitCost", unitcost);
            userEntity.PropertyList.Add("UnitPrice", unitprice);
            userEntity.PropertyList.Add("BrandName", brandname);
            return userEntity;
        }
    }

    public interface IGenericEntityRepository
    {
        IEnumerable<GenericEntity> GetEntities(string entityName);
    }

    public class User
    {
        public User(short employeeNumber, long universalId)
        {
            this.EmployeeNumber = employeeNumber;
            this.UniversalId = universalId;
        }

        public Guid id { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public int Age { get; set; }

        public decimal Salary { get; set; }

        public DateTime BornOn { get; set; }

        public string Department { get; set; }
        public string EmailAddress { get; set; }
        public short EmployeeNumber { get; set; }

        public long UniversalId { get; set; }

        public double VacationDaysInHours { get; set; }
    }
}
