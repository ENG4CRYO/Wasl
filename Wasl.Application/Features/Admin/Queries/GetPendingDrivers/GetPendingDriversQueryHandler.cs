using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wasl.Application.Common;
using Wasl.Application.Dtos.Admin;
using Wasl.Application.Interfaces.Common;
using Wasl.Core.Enums;

namespace Wasl.Application.Features.Admin.Queries.GetPendingDrivers
{
    public class GetPendingDriversQueryHandler : IRequestHandler<GetPendingDriversQuery, ApiResponse<PagedList<PendingDriverListDto>>>
    {
        private readonly IApplicationDbContext _context;

        public GetPendingDriversQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ApiResponse<PagedList<PendingDriverListDto>>> Handle(GetPendingDriversQuery request, CancellationToken cancellationToken)
        {
            var query = _context.DriverProfiles
                .Include(dp => dp.User)
                .Where(dp => dp.ApprovalStatus == DriverApprovalStatus.UnderReview)
                .AsNoTracking();

            var totalCount = await query.CountAsync(cancellationToken);


            var items = await query
                .OrderBy(dp => dp.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(dp => new PendingDriverListDto
                {
                    DriverId = dp.UserId,
                    FullName = dp.User.FirstName + " " + dp.User.LastName,
                    Email = dp.User.Email,
                    PhoneNumber = dp.User.PhoneNumber,
                    SubmittedAt = dp.CreatedAt
                })
                .ToListAsync(cancellationToken);

            var pagedList = new PagedList<PendingDriverListDto>(items, totalCount, request.PageNumber, request.PageSize);

            return ApiResponse<PagedList<PendingDriverListDto>>.Success(pagedList, "تم جلب قائمة السائقين بنجاح.");
        }
    }
}