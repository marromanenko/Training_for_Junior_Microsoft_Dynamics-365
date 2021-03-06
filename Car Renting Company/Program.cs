using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp.Enums;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;

namespace ConsoleApp
{
    public class Rent
    { 
        DateTime startDate, endDate, startRentDate, endRentDate;
        int range, st, statusCode, ran, ranc;
        private static Random gen = new Random();
        EntityReference car, carclass, contact;
        OptionSetValue pickuploc, returnloc;
        Entity newRent, newReport;
        Money mon;

        public static Guid getRandomCar(Dictionary<Guid, Guid> Cars, Guid carClass)
        {
            var newDictionary = Cars.Where(c => c.Value == carClass);
            return newDictionary.ElementAt(gen.Next(newDictionary.Count())).Key;
        }

        public Rent(List<Guid> guidsCarsClass, Dictionary<Guid, Guid> guidsCars, List<Guid> guidsContacts, IOrganizationService service)
        {
            newRent = new Entity("mr_rent");
            newReport = new Entity("mr_cartransferreport");
            startDate = new DateTime(2019, 1, 1);
            endDate = new DateTime(2020, 12, 1);
            range = (endDate - startDate).Days;
            startRentDate = startDate.AddDays(gen.Next(range));
            endRentDate = startRentDate.AddDays(gen.Next(30));
            ran = gen.Next(0, guidsCarsClass.Count);
            carclass = new EntityReference("mr_carclass", guidsCarsClass.ElementAt(ran));
            car = new EntityReference("mr_car", getRandomCar(guidsCars, guidsCarsClass.ElementAt(ran)));
            pickuploc = new OptionSetValue(gen.Next(315890000, 315890003));
            returnloc = new OptionSetValue(gen.Next(315890000, 315890003));
            ranc = gen.Next(0, guidsContacts.Count);
            contact = new EntityReference("contact", guidsContacts.ElementAt(gen.Next(ranc)));
            st = gen.Next(101);
            mon = new Money(gen.Next(500));
        }

        public Guid fillAllRents(IOrganizationService service, List<Guid> guidsCarsClass, Dictionary<Guid, Guid> guidsCars)
        { 
            newRent["mr_reservedpickup"] = startRentDate;
            newRent["mr_actualpickup"] = startRentDate;
            newRent["mr_reservedhandover"] = endRentDate;
            newRent["mr_actualreturn"] = endRentDate;
            newRent["mr_carclass"] = carclass;
            newRent["mr_car"] = car;
            newRent["mr_pickuplocation"] = pickuploc;
            newRent["mr_returnlocation"] = returnloc;
            newRent["mr_customer"] = contact;
            newRent["mr_price"] = mon;
            if (st < 6) //created
            {
                statusCode = (int)StatusCode.Created;
            }
            else if (st < 11) //confirmed
            {
                statusCode = (int)StatusCode.Confirmed;
            }
            else if (st < 16) //renting
            {
                statusCode = (int)StatusCode.Renting;
                newReport["mr_name"] = "Pickup";
                newReport["mr_type"] = false;
                newReport["mr_date"] = startRentDate;
                if (gen.Next(101) > 5)
                {
                    newReport["mr_damages"] = false;
                }
                else
                {
                    newReport["mr_damages"] = true;
                    newReport["mr_damagedescription"] = "damage";
                }
                newReport["mr_car"] = car;
                newRent["mr_pickupreport"] = new EntityReference("mr_cartransferreport", service.Create(newReport));
            }
            else if (st < 91) //returned
            {
                statusCode = (int)StatusCode.Returned;
                newReport["mr_name"] = "Pickup";
                newReport["mr_type"] = false;
                newReport["mr_date"] = startRentDate;
                if (gen.Next(101) > 5)
                {
                    newReport["mr_damages"] = false;
                }
                else
                {
                    newReport["mr_damages"] = true;
                    newReport["mr_damagedescription"] = "damage";
                }

                newReport["mr_car"] = car;
                newRent["mr_pickupreport"] = new EntityReference("mr_cartransferreport", service.Create(newReport));
                newReport["mr_name"] = "Return";
                newReport["mr_type"] = true;
                newReport["mr_date"] = endRentDate;

                if (gen.Next(101) > 5)
                {
                    newReport["mr_damages"] = false;
                    //newReport["mr_damagedescription"] = null;
                }
                else
                {
                    newReport["mr_damages"] = true;
                    newReport["mr_damagedescription"] = "damage";
                }
                newReport["mr_car"] = car;
                newRent["mr_returnreport"] = new EntityReference("mr_cartransferreport", service.Create(newReport));
            }
            else //canceled
            {
                statusCode = (int)StatusCode.Canceled;
            }

            var activeStatuses = new List<int> { (int)StatusCode.Created, (int)StatusCode.Confirmed, (int)StatusCode.Renting };
            if (activeStatuses.Contains(statusCode)) newRent["statecode"] = new OptionSetValue(0);
            else newRent["statecode"] = new OptionSetValue(1);
            newRent["statuscode"] = new OptionSetValue(statusCode);

            switch (statusCode)
            {
                case 315890000:
                    if (gen.Next(11) < 10) newRent["mr_paid"] = true;
                    else newRent["mr_paid"] = false;
                    break;
                case 315890001:
                    if (gen.Next(1001) < 1000) newRent["mr_paid"] = true;
                    else newRent["mr_paid"] = false;
                    break;
                case 315890004:
                    if (gen.Next(10001) < 9999) newRent["mr_paid"] = true;
                    else newRent["mr_paid"] = false;
                    break;
                default:
                    newRent["mr_paid"] = false;
                    break;
            }
            return service.Create(newRent);
        }

    }
    class Program
    {
        public static EntityCollection getCollection(IOrganizationService service, string entityName, params string[] columSets)
        {
            QueryExpression query = new QueryExpression
            {
                EntityName = entityName,
                ColumnSet = new ColumnSet(columSets)
            };
            return service.RetrieveMultiple(query);
        }

        public static Dictionary<Guid, Guid> GetEntityCar(IOrganizationService service)
        {
            EntityCollection collection = getCollection(service, "mr_car", "mr_name", "mr_carclass");
            return collection.Entities
                .Select(x => new { 
                    Car = x.Id, 
                    CarClass = x.GetAttributeValue<EntityReference>("mr_carclass").Id })
                .ToDictionary(x => x.Car, x => x.CarClass);
        }

        public static List<Guid> getListID(EntityCollection collection)
        {
            var listID = new List<Guid>();
            foreach (var a in collection.Entities)
            {
                listID.Add(a.Id);
            }
            return listID;
        }

        static void Main(string[] args)
        {
            string connectionString = @"AuthType=OAuth;
                                        Username=MariiaRomanenko@Sevent786.onmicrosoft.com;
                                        Password=Tase2249;
                                        Url=https://d365newenviroment.crm10.dynamics.com;
                                        AppId=51f81489-12ee-4a9e-aaae-a2591f45987d;
                                        RedirectUri=app://58145B91-0C36-4500-8554-080854F2AC97;";
            CrmServiceClient service = new CrmServiceClient(connectionString);

            EntityCollection collection1 = getCollection(service, "mr_carclass", "mr_carclassid");
            EntityCollection collection2 = getCollection(service, "contact", "contactid");

            List<Guid> guidsCarsClass = getListID(collection1);
            List<Guid> guidsContacts = getListID(collection2);

            Dictionary<Guid, Guid> guidsCars = GetEntityCar(service);

            for (int i = 0; i < 40000; i++)
            {
                new Rent(guidsCarsClass, guidsCars, guidsContacts, service).fillAllRents(service, guidsCarsClass, guidsCars);
                Console.WriteLine(i);
            }
        }
    }

}
