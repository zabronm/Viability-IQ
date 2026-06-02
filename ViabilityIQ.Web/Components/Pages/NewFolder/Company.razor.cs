
using BrucolWeb.Application.DTOs.Common;
using BrucolWeb.Application.DTOs.Company;
using BrucolWeb.Application.Services;
using BrucolWeb.Web.Components.Common;
using Microsoft.AspNetCore.Components;
using System.Data;
using System.Runtime.CompilerServices;


namespace BrucolWeb.Web.Components.Pages
{
    public partial class Company
    {
        [Inject] protected CompanyService? companyService { get; set; }
        [Inject] protected NavigationManager? Nav { get; set; }
        [Parameter] public EventCallback<CreateCompanyDTO> OnSave { get; set; }

        private CreateCompanyDTO _model = new CreateCompanyDTO();
        private List<CompanyListDTO> companies = new();
        private ZabOffCanvas? offcanvas;
        private string offcanvasTitle = "Add Company";
        private bool isSaving = false;
        private string? ButtonMessage;

        List<ZabDataTable<CompanyListDTO>.ColumnDefinition<CompanyListDTO>> columns = new()
        {
            new() { Title = "Company Name", Value = x => x.CompanyName },           
            new() { Title = "CK Number", Value = x => x.CKNumber },
            new() { Title = "Province", Value = x => x.ProvinceName },
            new() { Title = "Contact Person", Value = x => x.ContactPerson },
            new() { Title = "Telephone", Value = x => x.Telephone },
            new() { Title = "Phone", Value = x => x.Mobile },
            new() { Title = "Email", Value = x => x.Email },
            new() { Title = "Website", Value = x => x.Website },            
        };


        protected override async Task OnInitializedAsync()
        {
            try
            {
                var result = await companyService.GetListAsync();
                companies = result.ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // later: show toast notification
            }
        }

        async Task CreateCompany()
        {
            //Nav?.NavigateTo("/FarmPages/create");
            ButtonMessage = "Create Company";
            offcanvasTitle = "Add Company";
            _model = new CreateCompanyDTO();
            await offcanvas.Show();
        }

        void EditCompanyDetails(long id) => Nav.NavigateTo($"/companyPages/edit/{id}");


        //EDIT COMPANY IN OFF-CANVAS
        async Task EditCompany(CompanyListDTO company)
        {
            //EditFarmDetails(farm.Id);
            ButtonMessage = "Update Company";
            offcanvasTitle = "Edit Company";
            _model = new CreateCompanyDTO
            {
                //CompanyId = company.Id,
                CompanyName = company.CompanyName,
                CKNumber = company.CKNumber,
                ContactPerson = company.ContactPerson,
                Street_Address = company.Street_Address,
                Suburb = company.Suburb,
                CityTown = company.CityTown,
                ProvinceId = company.ProvinceId,
                Country = company.Country,
                Postal_Address = company.Postal_Address,
                Postal_CityTown = company.Postal_CityTown,
                PostalCode = company.PostalCode,
                Email = company.Email,
                Telephone = company.Telephone,
                Mobile = company.Mobile,               
                Website = company.Website,
                Active = company.Active,

            };

            await offcanvas.Show();
        }

        async Task DeleteCompany(CompanyListDTO company)
        {
            try
            {
                //await companyService.ArchiveCompany(company.Id);
                //companies.Remove(company);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // later: show toast notification
            }
        }

        //SAVE COMPANY CODE
        async Task SaveCompany()
        {
            try
            {
                //await companyService.ArchiveCompany(company.Id);
                //companies.Remove(company);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // later: show toast notification
            }
        }

        private async Task HandleSubmit()
        {
            isSaving = true;

            await OnSave.InvokeAsync(_model);

            isSaving = false;
        }

    }
}
