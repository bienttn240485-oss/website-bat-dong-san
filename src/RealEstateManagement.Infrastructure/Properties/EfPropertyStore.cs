using RealEstateManagement.Application.Properties;
using RealEstateManagement.Domain.Properties;
using RealEstateManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace RealEstateManagement.Infrastructure.Properties;

public sealed class EfPropertyStore(ApplicationDbContext dbContext) : IPropertyStore
{
    public async Task<IReadOnlyList<PropertySummaryDto>> ListPropertiesAsync(PropertyFilterQuery query, CancellationToken cancellationToken)
    {
        var properties = await ApplyFilter(dbContext.Properties.AsNoTracking(), query)
            .Include(property => property.Images)
            .Include(property => property.Amenities)
            .OrderBy(property => property.Code)
            .ToListAsync(cancellationToken);

        return properties.Select(ToSummaryDto).ToArray();
    }

    public async Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken)
    {
        var property = await QueryProperties()
            .FirstOrDefaultAsync(property => property.Id == id, cancellationToken);

        return property is null ? null : ToDetailDto(property);
    }

    public async Task<PropertyFilterOptionsDto> GetFilterOptionsAsync(PropertyFilterQuery query, CancellationToken cancellationToken)
    {
        var properties = await ApplyFilter(dbContext.Properties.AsNoTracking(), query with
            {
                Project = null,
                Area = null,
                Type = null,
                Status = null,
                Keyword = null
            })
            .Select(property => new
            {
                property.Project,
                property.Area,
                property.Type,
                property.Status
            })
            .ToListAsync(cancellationToken);

        return new PropertyFilterOptionsDto(
            properties.Select(property => property.Project).Where(project => project is not null).Select(project => project!.Value).Distinct().OrderBy(project => project).ToArray(),
            properties.Where(property => !string.IsNullOrWhiteSpace(property.Area)).Select(property => new PropertyAreaOptionDto(property.Project, property.Area)).Distinct().OrderBy(area => area.Area).ToArray(),
            properties.Select(property => property.Type).Distinct().OrderBy(type => type).ToArray(),
            properties.Select(property => property.Status).Distinct().OrderBy(status => status).ToArray());
    }

    public Task<Property?> GetPropertyForUpdateAsync(Guid id, CancellationToken cancellationToken)
        => dbContext.Properties
            .FirstOrDefaultAsync(property => property.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string normalizedCode, Guid? exceptPropertyId, CancellationToken cancellationToken)
        => dbContext.Properties.AnyAsync(
            property => property.Code == normalizedCode && (exceptPropertyId == null || property.Id != exceptPropertyId.Value),
            cancellationToken);

    public async Task<bool> HasContractRelationshipsAsync(Guid propertyId, CancellationToken cancellationToken)
        => await dbContext.LandlordContracts.AnyAsync(contract => contract.PropertyId == propertyId, cancellationToken)
            || await dbContext.TenantContracts.AnyAsync(contract => contract.PropertyId == propertyId, cancellationToken);

    public async Task ClearPropertyChildrenAsync(Guid propertyId, CancellationToken cancellationToken)
    {
        await dbContext.PropertyImages
            .Where(image => image.PropertyId == propertyId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.PropertyFurnitureItems
            .Where(item => item.PropertyId == propertyId)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.PropertyAmenities
            .Where(amenity => amenity.PropertyId == propertyId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AddPropertyAsync(Property property, CancellationToken cancellationToken)
        => await dbContext.Properties.AddAsync(property, cancellationToken);

    public void DeleteProperty(Property property)
        => dbContext.Properties.Remove(property);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        MarkReplacementChildrenAsAdded();
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private void MarkReplacementChildrenAsAdded()
    {
        foreach (var entry in dbContext.ChangeTracker.Entries<PropertyImage>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<PropertyFurnitureItem>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<PropertyAmenity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.State = EntityState.Added;
            }
        }
    }

    private IQueryable<Property> QueryProperties()
        => dbContext.Properties
            .AsNoTracking()
            .Include(property => property.Images)
            .Include(property => property.FurnitureItems)
            .Include(property => property.Amenities);

    private static IQueryable<Property> ApplyFilter(IQueryable<Property> queryable, PropertyFilterQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim();
            queryable = queryable.Where(property =>
                property.Code.Contains(keyword)
                || property.Area.Contains(keyword)
                || (property.Description != null && property.Description.Contains(keyword)));
        }

        if (query.Project is not null)
        {
            queryable = queryable.Where(property => property.Project == query.Project);
        }

        if (!string.IsNullOrWhiteSpace(query.Area))
        {
            queryable = queryable.Where(property => property.Area == query.Area.Trim());
        }

        if (query.Type is not null)
        {
            queryable = queryable.Where(property => property.Type == query.Type);
        }

        if (query.Status is not null)
        {
            queryable = queryable.Where(property => property.Status == query.Status);
        }

        if (query.MinMonthlyPrice is not null)
        {
            queryable = queryable.Where(property => property.MonthlyPrice >= query.MinMonthlyPrice.Value);
        }

        if (query.MaxMonthlyPrice is not null)
        {
            queryable = queryable.Where(property => property.MonthlyPrice <= query.MaxMonthlyPrice.Value);
        }

        if (query.MinSalePrice is not null)
        {
            queryable = queryable.Where(property => property.SalePrice >= query.MinSalePrice.Value);
        }

        if (query.MaxSalePrice is not null)
        {
            queryable = queryable.Where(property => property.SalePrice <= query.MaxSalePrice.Value);
        }

        if (query.SalesOnly)
        {
            queryable = queryable.Where(property => property.SalePrice > 0);
        }

        return queryable;
    }

    private static PropertySummaryDto ToSummaryDto(Property property)
    {
        var primaryImage = property.Images.OrderBy(image => image.SortOrder).FirstOrDefault();
        return new PropertySummaryDto(
            property.Id,
            property.Code,
            property.Project,
            property.Area,
            property.Type,
            property.AreaSize,
            property.Bathrooms,
            property.MonthlyPrice,
            property.SalePrice,
            property.Direction,
            property.FurniturePackage,
            property.Status,
            property.AvailableFromDate,
            primaryImage?.Url,
            property.Amenities.OrderBy(amenity => amenity.Name).Select(amenity => amenity.Name).ToArray(),
            property.CreatedAtUtc);
    }

    private static PropertyDetailDto ToDetailDto(Property property)
        => new(
            property.Id,
            property.Code,
            property.Project,
            property.Area,
            property.Type,
            property.AreaSize,
            property.Bathrooms,
            property.MonthlyPrice,
            property.SalePrice,
            property.Direction,
            property.LoanInfo,
            property.LegalStatus,
            property.FurniturePackage,
            property.Description,
            property.VideoUrl,
            property.Status,
            property.AvailableFromDate,
            property.Notes,
            property.CreatedAtUtc,
            property.UpdatedAtUtc,
            property.Images.OrderBy(image => image.SortOrder).Select(image => new PropertyImageDto(image.Id, image.Url, image.AltText, image.SortOrder, image.IsPrimary)).ToArray(),
            property.FurnitureItems.OrderBy(item => item.Name).Select(item => new PropertyFurnitureItemDto(item.Id, item.Name, item.Quantity, item.Notes)).ToArray(),
            property.Amenities.OrderBy(amenity => amenity.Name).Select(amenity => new PropertyAmenityDto(amenity.Id, amenity.Name)).ToArray());
}
