 using ACC.ViewModels;
using ACC.ViewModels.ProjectActivityVM;
using BusinessLogic.Repository.RepositoryClasses;
using BusinessLogic.Repository.RepositoryInterfaces;
using DataLayer.Models.Enums;
using DataLayer.Models;
using Microsoft.AspNetCore.Mvc;
using System.Drawing.Printing;
using NuGet.Protocol.Core.Types;
using DataLayer.Models.Enums.ProjectActivity;
using Helpers;

namespace ACC.Controllers.ProjectDetailsController
{
    public class ProjectActivitiesController : Controller
    {
        private readonly IProjectActivityRepository activityRepository;

        public ProjectActivitiesController(IProjectActivityRepository _activityRepository)
        {
            activityRepository = _activityRepository;
        }



        public IActionResult Index(int id , FilterVM filter , int page = 1, int pageSize = 4)
        
        {
            var query = activityRepository.GetByProjectId(id);

            if (!string.IsNullOrEmpty(filter.memberName))
            {
                query = query.Where(a => a.UserEmail == filter.memberName).ToList();
            }

            if (!string.IsNullOrEmpty(filter.activityType))
            {
                query = query.Where(a => a.ActivityType == filter.activityType).ToList();
            }

            if (filter.startDate.HasValue)
            {
                query = query.Where(a => a.Date >= filter.startDate.Value).ToList();
            }

            if (filter.endDate.HasValue)
            {
                query = query.Where(a => a.Date <= filter.endDate.Value).ToList();
            }

            int totalRecords = query.Count();

            var ActivitiesListModel = query
                .Select(a => new ProjectActivityVM
                {
                    Id = a.Id,
                    Date = a.Date,
                    ActivityType = a.ActivityType,
                    ActivityDetail = a.ActivityDetail,
                    ProjectId = a.projectId??0,
                    UserEmail = a.UserEmail,
                })
                .Skip((page - 1) * pageSize)  
                .Take(pageSize)  
                .ToList();

            var filterLists = new FilterVM
            {
                activityTypesList = activityRepository.GetAll().Where(i=>i.projectId==id).Select(i=>i.ActivityType).Distinct().ToList(),
                memberNamesList = activityRepository.GetAll().Where(i => i.projectId == id).Select(i=>i.UserEmail).Distinct().ToList(),
            };
            ViewBag.filterLists = filterLists;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.ActivityTypeFilter = filter.activityType;
            ViewBag.StartDateFilter = filter.startDate;
            ViewBag.EndDateFilter = filter.endDate;

            ViewBag.id = id;

            return View("Index", ActivitiesListModel);
        }






    }
}
