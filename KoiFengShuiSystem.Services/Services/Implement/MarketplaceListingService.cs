using CloudinaryDotNet.Actions;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class MarketplaceListingService : IMarketplaceListingService
    {
        private readonly UnitOfWorkRepository _unitOfWork;
        private readonly GenericRepository<Account> _accountRepository;
        private readonly ICloudService _cloudService;

        public MarketplaceListingService(ICloudService cloudService)
        {
            _unitOfWork = new UnitOfWorkRepository();
            _accountRepository = new GenericRepository<Account>();
            _cloudService = cloudService;
        }

        public async Task<IBusinessResult> CreateMarketplaceListing(MarketplaceListingRequest marketplaceListing, List<IFormFile> imgFiles, int userId)
        {
            try
            {
                //var entityInDb = await _unitOfWork.MarketplaceListingRepository.GetByIdAsync((int)marketplaceListing.ListingId);

                //// If Post already exists, return an error indicating duplicate Post
                //if (entityInDb != null)
                //{
                //    return new BusinessResult(Const.WARNING_NO_DATA_CODE, "Post already exists. Cannot create a new Post with the same ID.");
                //}
                var newMarketPlaceListing = new MarketplaceListing
                {
                    AccountId = userId,
                    TierId = marketplaceListing.TierId,
                    Title = marketplaceListing.Title,
                    Description = marketplaceListing.Description,
                    Price = marketplaceListing.Price,
                    Quantity = marketplaceListing.Quantity,
                    CategoryId = marketplaceListing.CategoryId,
                    CreateAt = DateTime.Now,
                    ExpiresAt = (DateTime)marketplaceListing.ExpiresAt,
                    //ListingImages = mp.ListingImages,
                    Color = marketplaceListing.Color,
                    IsActive = marketplaceListing.IsActive,
                    ElementId = marketplaceListing.ElementId,
                    Status = "Approved",//Change to Pending or Approved when have ApporveMKPL
                };
                // Upload images using CloudService
                List<ImageUploadResult> uploadResults = await _cloudService.UploadImagesAsync(imgFiles);
                // Handle adding new HomeImages with the uploaded URLs
                if (uploadResults.Any())
                {
                    foreach (var uploadResult in uploadResults)
                    {
                        var homeImageEntity = new ListingImage
                        {
                            Image = new Image
                            {
                                ImageUrl = uploadResult.SecureUrl.ToString()
                            },
                            ImageDescription = null // Optionally add descriptions here
                        };
                        newMarketPlaceListing.ListingImages.Add(homeImageEntity);
                    }
                }
                // If Post doesn't exist, create a new one
                await _unitOfWork.MarketplaceListingRepository.CreateAsync(newMarketPlaceListing);
                return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
            }
            catch (Exception ex)
            {
                return new BusinessResult(-4, ex.Message);
            }
        }

        public async Task<IBusinessResult> DeleteMarketplaceListing(int id)
        {
            try
            {
                var marketplaceListing = await _unitOfWork.MarketplaceListingRepository.GetByIdAsync(id);
                if (marketplaceListing != null)
                {
                    var result = await _unitOfWork.MarketplaceListingRepository.RemoveAsync(marketplaceListing);
                    if (result)
                    {
                        return new BusinessResult(Const.SUCCESS_DELETE_CODE, Const.SUCCESS_DELETE_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.FAIL_DELETE_CODE, Const.FAIL_DELETE_MSG);
                    }
                }
                else
                {
                    return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.WARNING_NO_DATA__MSG);
                }
            }
            catch (Exception ex)
            {
                return new BusinessResult(-4, ex.ToString());
            }
        }

        public async Task<IBusinessResult> GetAll()
        {
            try
            {
                var marketplaceListings = await _unitOfWork.MarketplaceListingRepository.GetAllWithElementAsync();
                if (marketplaceListings != null)
                {
                    var marketplaceListingsResponses = marketplaceListings.Select(mp => new MarketplaceListingResponse
                    {
                        AccountId = mp.AccountId,
                        TierId = mp.TierId,
                        Title = mp.Title,
                        Description = mp.Description,
                        Price = mp.Price,
                        Quantity = mp.Quantity,
                        CategoryId = mp.CategoryId,
                        CreateAt = DateTime.Now,
                        ExpiresAt = mp.ExpiresAt,
                        //ListingImages = mp.ListingImages,
                        Color = mp.Color,
                        IsActive = mp.IsActive,
                        ElementId = mp.ElementId,
                        AccountName = mp.Account.FullName,
                        ElementName = mp.Element?.ElementName,
                        TierName = mp.Tier.TierName,
                        Status = mp.Status,
                    }).ToList();
                    if (marketplaceListingsResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, marketplaceListingsResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }

        public async Task<IBusinessResult> GetMarketplaceListingByAccountId(int accountId, int categoryId, int excludeListingId, int pageNumber, int pageSize)
        {
            try
            {
                var marketplaceListings = await _unitOfWork.MarketplaceListingRepository.GetAllByAccountIdAsync(accountId, categoryId, excludeListingId, pageNumber, pageSize);
                if (marketplaceListings != null)
                {
                    var marketplaceListingsResponses = marketplaceListings.Select(mp => new MarketplaceListingResponse
                    {
                        ListingId = mp.ListingId,
                        AccountId = mp.AccountId,
                        TierId = mp.TierId,
                        Title = mp.Title,
                        Description = mp.Description,
                        Price = mp.Price,
                        Quantity = mp.Quantity,
                        CategoryId = mp.CategoryId,
                        CreateAt = DateTime.Now,
                        ExpiresAt = mp.ExpiresAt,
                        Color = mp.Color,
                        IsActive = mp.IsActive,
                        ElementId = mp.ElementId,
                        AccountName = mp.Account.FullName,
                        ElementName = mp.Element?.ElementName,
                        TierName = mp.Tier.TierName,
                        Status = mp.Status,
                    }).ToList();
                    if (marketplaceListingsResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, marketplaceListingsResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }

        public async Task<IBusinessResult> GetMarketplaceListingByCategoryId(int categoryId, int pageNumber, int pageSize)
        {
            try
            {
                var marketplaceListings = await _unitOfWork.MarketplaceListingRepository.GetAllByCategoryTypeIdAsync(categoryId, pageNumber, pageSize);
                if (marketplaceListings != null)
                {
                    var marketplaceListingsResponses = marketplaceListings.Select(mp => new MarketplaceListingResponse
                    {
                        ListingId = mp.ListingId,
                        AccountId = mp.AccountId,
                        TierId = mp.TierId,
                        Title = mp.Title,
                        Description = mp.Description,
                        Price = mp.Price,
                        Quantity = mp.Quantity,
                        CategoryId = mp.CategoryId,
                        CreateAt = DateTime.Now,
                        ExpiresAt = mp.ExpiresAt,
                        Color = mp.Color,
                        IsActive = mp.IsActive,
                        ElementId = mp.ElementId,
                        AccountName = mp.Account.FullName,
                        ElementName = mp.Element?.ElementName,
                        TierName = mp.Tier.TierName,
                        Status = mp.Status,
                    }).ToList();
                    if (marketplaceListingsResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, marketplaceListingsResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }

        public async Task<IBusinessResult> GetMarketplaceListingByElementId(int elementId, int categoryId, int excludeListingId, int pageNumber, int pageSize)
        {
            try
            {
                var marketplaceListings = await _unitOfWork.MarketplaceListingRepository.GetAllByElementIdAsync(elementId, categoryId, excludeListingId, pageNumber, pageSize);
                if (marketplaceListings != null)
                {
                    var marketplaceListingsResponses = marketplaceListings.Select(mp => new MarketplaceListingResponse
                    {
                        ListingId = mp.ListingId,
                        AccountId = mp.AccountId,
                        TierId = mp.TierId,
                        Title = mp.Title,
                        Description = mp.Description,
                        Price = mp.Price,
                        Quantity = mp.Quantity,
                        CategoryId = mp.CategoryId,
                        CreateAt = DateTime.Now,
                        ExpiresAt = mp.ExpiresAt,
                        Color = mp.Color,
                        IsActive = mp.IsActive,
                        ElementId = mp.ElementId,
                        AccountName = mp.Account.FullName,
                        ElementName = mp.Element?.ElementName,
                        TierName = mp.Tier.TierName,
                        Status = mp.Status,
                    }).ToList();
                    if (marketplaceListingsResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, marketplaceListingsResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }

        public async Task<IBusinessResult> GetMarketplaceListingById(int id)
        {
            try
            {
                var marketplaceListings = await _unitOfWork.MarketplaceListingRepository.GetAllByCategoryByIdAsync(id);

                if (marketplaceListings != null)
                {
                    var marketplaceListingsResponses = marketplaceListings.Select(mp => new MarketplaceListingResponse
                    {
                        ListingId = mp.ListingId,
                        AccountId = mp.AccountId,
                        TierId = mp.TierId,
                        Title = mp.Title,
                        Description = mp.Description,
                        Price = mp.Price,
                        Quantity = mp.Quantity,
                        Color = mp.Color,
                        CategoryId = mp.CategoryId,
                        CreateAt = DateTime.Now,
                        ExpiresAt = mp.ExpiresAt,
                        IsActive = mp.IsActive,
                        ElementId = mp.ElementId,
                        AccountName = mp.Account.FullName,
                        AccountPhoneNumber = mp.Account.Phone,
                        ElementName = mp.Element?.ElementName,
                        TierName = mp.Tier.TierName,
                        Status = mp.Status,
                    }).ToList();
                    if (marketplaceListingsResponses == null)
                    {
                        return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);
                    }
                    else
                    {
                        return new BusinessResult(Const.SUCCESS_READ_CODE, Const.SUCCESS_READ_MSG, marketplaceListingsResponses);
                    }
                }
                return new BusinessResult(Const.WARNING_NO_DATA_CODE, Const.FAIL_READ_MSG);

            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }

        public async Task<IBusinessResult> Save()
        {
            try
            {
                var result = await _unitOfWork.MarketplaceListingRepository.SaveAsync();
                if (result > 0)
                {
                    return new BusinessResult(Const.SUCCESS_CREATE_CODE, Const.SUCCESS_CREATE_MSG);
                }
                else
                {
                    return new BusinessResult(Const.FAIL_CREATE_CODE, Const.FAIL_CREATE_MSG);
                }
            }
            catch (Exception e)
            {
                return new BusinessResult(-4, e.Message.ToString());
            }
        }
    }
}
