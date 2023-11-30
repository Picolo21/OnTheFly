using MongoDB.Driver;
using OnTheFly.Models;

namespace OnTheFly.Connections;

public class CompanyConnection
{
    private IMongoDatabase _dataBase;

    public CompanyConnection()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        _dataBase = client.GetDatabase("Company");
    }

    public async Task<Company> InsertAsync(Company company)
    {
        var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
        collection.InsertOne(company);
        var companyResult = await collection.Find(c => c.Cnpj == company.Cnpj).FirstOrDefaultAsync();
        return companyResult;
    }

    public async Task<List<Company>> FindAllAsync()
    {
        var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
        return await collection.Find(x => true).ToListAsync();
    }

    public async Task<Company> FindByCnpjAsync(string cnpj)
    {
        var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
        return await collection.Find(x => x.Cnpj == cnpj).FirstOrDefaultAsync();
    }

    public async Task<Company> FindByCnpjRestrictedAsync(string cnpj)
    {
        var collection = _dataBase.GetCollection<Company>("RestrictedCompanies");
        return await collection.Find(x => x.Cnpj == cnpj).FirstOrDefaultAsync();
    }

    public async Task<Company> FindByCnpjDeletedAsync(string cnpj)
    {
        var collection = _dataBase.GetCollection<Company>("DeletedCompanies");
        return await collection.Find(x => x.Cnpj == cnpj).FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAsync(string cnpj)
    {
        try
        {
            var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
            var collectioRestricted = _dataBase.GetCollection<Company>("RestrictedCompanies");
            var collectionDeleted = _dataBase.GetCollection<Company>("DeletedCompanies");

            var trash = await collection.FindOneAndDeleteAsync(c => c.Cnpj == cnpj);
            if (trash == null)
            {
                var trashRestricted = await collectioRestricted.FindOneAndDeleteAsync(c => c.Cnpj == cnpj);
                if (trashRestricted == null)
                    return false;
                else
                {
                    await collectionDeleted.InsertOneAsync(trashRestricted);
                    return true;
                }
            }
            else
            {
                await collectionDeleted.InsertOneAsync(trash);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestrictAsync(string cnpj)
    {
        try
        {
            var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
            var collectioRestricted = _dataBase.GetCollection<Company>("RestrictedCompanies");

            var company = await collection.FindOneAndDeleteAsync(c => c.Cnpj == cnpj);
            if (company == null)
                return false;
            else
            {
                await collectioRestricted.InsertOneAsync(company);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnrestrictAsync(string cnpj)
    {
        try
        {
            var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
            var collectioRestricted = _dataBase.GetCollection<Company>("RestrictedCompanies");

            var restricted = await collectioRestricted.FindOneAndDeleteAsync(c => c.Cnpj == cnpj);
            if (restricted == null)
                return false;
            else
            { 
                await collection.InsertOneAsync(restricted);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UndeleteCompanyAsync(string cnpj)
    {
        try
        {
            var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
            var collectioDeleted = _dataBase.GetCollection<Company>("DeletedCompanies");

            var deleted = await collectioDeleted.FindOneAndDeleteAsync(c => c.Cnpj == cnpj);
            if (deleted == null)
                return false;
            else
            {
                await collection.InsertOneAsync(deleted);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public bool Update(string cnpj, Company company)
    {
        var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
        return collection.ReplaceOne(c=> c.Cnpj==cnpj, company).IsAcknowledged;
    }

    public async Task UpdateStatusAsync(string cnpj, bool status)
    {
        var filter = Builders<Company>.Filter.Eq("Cnpj", cnpj);
        var update = Builders<Company>.Update.Set("Status", status);

        var collection = _dataBase.GetCollection<Company>("ActivatedCompanies");
        await collection.UpdateOneAsync(filter, update);
    }
}