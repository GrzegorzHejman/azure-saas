﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Saas.SignupAdministration.Web.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Saas.SignupAdministration.Web.Services;
using Saas.SignupAdministration.Web.Services.StateMachine;

namespace Saas.SignupAdministration.Web.Controllers
{
    public class OnboardingWorkflowController : Controller
    {
        private readonly ILogger<OnboardingWorkflowController> _logger;
        private readonly AppSettings _appSettings;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly OnboardingWorkflow _onboardingWorkflow;

        public OnboardingWorkflowController(ILogger<OnboardingWorkflowController> logger, IOptions<AppSettings> appSettings, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, OnboardingWorkflow onboardingWorkflow)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _userManager = userManager;
            _signInManager = signInManager;
            _onboardingWorkflow = onboardingWorkflow;
        }

        // Step 1 - Submit the organization name
        [HttpGet]
        public IActionResult OrganizationName()
        {
            return View();
        }

        // Step 1 - Submit the organization name
        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult OrganizationName(string organizationName)
        {
            _onboardingWorkflow.OnboardingWorkflowItem.OrganizationName = organizationName;
            UpdateOnboardingSessionAndTransitionState(OnboardingWorkflowState.Triggers.OnOrganizationNamePosted);

            return RedirectToAction(SR.OrganizationCategoryAction, SR.OnboardingWorkflowController);
        }

        // Step 2 - Organization Category
        [Route(SR.OnboardingWorkflowOrganizationCategoryRoute)]
        [HttpGet]
        public IActionResult OrganizationCategory()
        {
            // Populate Categories dropdown list
            List<Category> categories = new List<Category>();

            categories.Add(new Category { Id = 1, Name = SR.AutomotiveMobilityAndTransportationPrompt });
            categories.Add(new Category { Id = 2, Name = SR.EnergyAndSustainabilityPrompt });
            categories.Add(new Category { Id = 3, Name = SR.FinancialServicesPrompt });
            categories.Add(new Category { Id = 4, Name = SR.HealthcareAndLifeSciencesPrompt });
            categories.Add(new Category { Id = 5, Name = SR.ManufacturingAndSupplyChainPrompt });
            categories.Add(new Category { Id = 6, Name = SR.MediaAndCommunicationsPrompt });
            categories.Add(new Category { Id = 7, Name = SR.PublicSectorPrompt });
            categories.Add(new Category { Id = 8, Name = SR.RetailAndConsumerGoodsPrompt });
            categories.Add(new Category { Id = 9, Name = SR.SoftwarePrompt });

            return View(categories);
        }

        // Step 2 Submitted - Organization Category
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult OrganizationCategoryAsync(int categoryId)
        {
            _onboardingWorkflow.OnboardingWorkflowItem.CategoryId = categoryId;
            UpdateOnboardingSessionAndTransitionState(OnboardingWorkflowState.Triggers.OnOrganizationCategoryPosted);

            return RedirectToAction(SR.TenantRouteNameAction, SR.OnboardingWorkflowController);
        }

        // Step 3 - Tenant Route Name
        [HttpGet]
        public IActionResult TenantRouteName()
        {
            return View();
        }

        // Step 3 Submitted - Tenant Route Name
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TenantRouteName(string tenantRouteName)
        {
            // TODO:Need to check whether the route name exists
            _onboardingWorkflow.OnboardingWorkflowItem.TenantRouteName = tenantRouteName;
            UpdateOnboardingSessionAndTransitionState(OnboardingWorkflowState.Triggers.OnTenantRouteNamePosted);

            return RedirectToAction(SR.ServicePlansAction, SR.OnboardingWorkflowController);
        }

        // Step 4 - Service Plan
        [HttpGet]
        public IActionResult ServicePlans()
        {
            return View();
        }

        // Step 4 Submitted - Service Plan
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ServicePlans(int productId)
        {
            _onboardingWorkflow.OnboardingWorkflowItem.ProductId = productId;
            UpdateOnboardingSessionAndTransitionState(OnboardingWorkflowState.Triggers.OnServicePlanPosted);

            return RedirectToAction(SR.ConfirmationAction, SR.OnboardingWorkflowController);
        }

        // Step 5 - Tenant Created Confirmation
        [HttpGet]
        public async Task<IActionResult> Confirmation()
        {
            // Deploy the Tenant
            await DeployTenantAsync();

            return View();
        }

        private async Task DeployTenantAsync()
        {
            _onboardingWorkflow.OnboardingWorkflowItem.IpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString(); ;

            await _onboardingWorkflow.OnboardTenet();

            UpdateOnboardingSessionAndTransitionState(OnboardingWorkflowState.Triggers.OnTenantDeploymentSuccessful);
        }

        private void UpdateOnboardingSessionAndTransitionState(OnboardingWorkflowState.Triggers trigger)
        {
            _onboardingWorkflow.TransitionState(trigger);
            _onboardingWorkflow.PersistToSession();
        }
    }
}
