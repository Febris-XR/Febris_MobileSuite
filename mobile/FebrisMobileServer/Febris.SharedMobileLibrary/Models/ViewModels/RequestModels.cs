// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Models.Data;
using Febris.ModelLibrary.Models.XApiModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Febris.SharedMobileLibrary.Models.ViewModels
{
    #region Authentication Models
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class HardwareInitializationRequest
    {
        public string UniqueIdentifier { get; set; }
    }



    #endregion


    #region Full Hardware Initialization 
    public class HardwareInitializationResponse
    {
        public HardwareInitializationResponse()
        {
            MessageboardViewModels = default;
            UserInitaliztionViewModels = default;
            ModuleInitaliztionViewModels = default;
            ModuleList = default;
        }

        public HardwareMessageboardViewModels MessageboardViewModels { get; set; }
        public HardwareUserInitaliztionViewModels UserInitaliztionViewModels { get; set; }
        public HardwareModuleInitaliztionViewModels ModuleInitaliztionViewModels { get; set; }
        public List<Module> ModuleList { get; set; }

    }



    #endregion


    #region Start Simulation 
    public class StatementInitalizationRequestViewModel
    {
        public Guid UserId { get; set; }
        public Guid ActorId { get; set; }
        public Guid ModuleId { get; set; }
        public bool IsTestUser { get; set; }
        public bool RecordSession { get; set; }
    }
    public class StatementInitalizationResponseViewModel
    {
        public Statement Statement { get; set; }
    }
    #endregion

    #region Statement Upload
    public class StatementUploadRequestViewModel
    {
        public Statement Statement { get; set; }
    }
    public class StatementUploadResponseViewModel
    {
        public bool Success { get; set; }

    }
    #endregion


    #region Direct Response ViewModels
    public class HardwareMessageboardViewModels
    {
        public HardwareMessageboardViewModels()
        {
            AdminMessageBoardList = default;
            MessageBoardList = default;
        }
        public List<AdminMessageBoard> AdminMessageBoardList { get; set; }
        public List<MessageBoard> MessageBoardList { get; set; }
    }

    public class HardwareUserInitaliztionViewModels
    {
        public HardwareUserInitaliztionViewModels()
        {
            CohortList = default;
            CohortMemberList = default;
            UserViewModelList = default;
        }
        public List<Cohort> CohortList { get; set; }
        public List<CohortMember> CohortMemberList { get; set; }
        public List<HardwareUserViewModel> UserViewModelList { get; set; }
        public List<UserAccessList> UserAccessLists { get; set; }
    }

    public class HardwareModuleInitaliztionViewModels
    {
        public HardwareModuleInitaliztionViewModels()
        {
            //CurriculumList = default;
            //ModuleList = default;
            ModuleLinkedCurriculumList = default;
        }
        //public List<Curriculum> CurriculumList { get; set; }
        //public List<Module> ModuleList { get; set; }
        public List<ModuleLinkedCurriculum> ModuleLinkedCurriculumList { get; set; }
    }
    #endregion


    #region Widget request models

    public class VideoFile
    {
        public string FileName { get; set; }
        public string TempFolder { get; set; }
        public int MaxFileSizeMB { get; set; }
        public List<string> FileParts { get; set; }
    }
    public class VideoFileUploadResponseViewModel
    {
        public bool Success { get; set; }

    }



    #endregion


    #region View Models 

    public class HardwareUserViewModel
    {
        public HardwareUserViewModel()
        {
            IsTestUser = false;
        }
        public Guid UserId { get; set; }
        public Guid ActorId { get; set; }
        public string IdentificationNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmailAddress { get; set; }
        public string ProfilePicturePath { get; set; }
        public string PicturePath { get; set; }
        public bool IsTestUser { get; set; }

        //public HardwareUserAccessViewModel AccessViewModel { get; set; }
    }

    public class UserAccessList
    {
        public UserAccessList(Guid UserId, Guid ActorId)
        {
            UserId = default;
            ActorId = default;
            ModuleIdList = default;
        }
        public Guid UserId { get; set; }
        public Guid ActorId { get; set; }
        public List<Guid> ModuleIdList { get; set; }
    }

    #endregion

    //class XApiViewModels
    //{
    //}
    //public class StatementDetailsViewModel
    //{
    //    public Statement Statement { get; set; }
    //    //public Professional Professional { get; set; }
    //    //public ApplicationUser ApplicationUser { get; set; }
    //    public Module Module { get; set; }
    //    public string Video { get; set; }
    //   // public XApiResultExtras xApiResultExtras { get; set; }
    //}

    //public class StatementInitializerGetViewModel
    //{
    //    //public Professional Professional { get; set; }
    //    public Module ModuleBase { get; set; }
    //}
    //public class StatementVoidingViewModel
    //{
    //    public Statement Statement { get; set; }
    //    //public Professional Professional { get; set; }
    //    public Module Module { get; set; }
    //}
    //public class VerbCreationViewModel
    //{
    //    public Verb Verb { get; set; }
    //    //[Display(Name = "Select main display language")]
    //    public LanguageMapTypeEnum LanguageMap { get; set; }
    //    [Display(Name = "Description in main language")]
    //    public string Description { get; set; }

    //}

    //public class StatementDataViewModel
    //{
    //    public Statement Statement { get; set; }
    //    public XApiResultExtras XApiResultExtras { get; set; }
    //}

    //public class XApiResultExtrasViewModel
    //{
    //    public RadarChart RadarChart { get; set; }
    //    public XApiResultExtras XApiResultExtras { get; set; }
    //}

    //public class HardwareAuthenticationRequest : BaseAuthenticationRequest
    //{
    //    [Required]
    //    public string LicenseKey { get; set; }
    //}

    //public class HardwareAuthenticationResponse : BaseAuthenticateResponse
    //{

    //    //public string JwtToken { get; set; }

    //    //[JsonIgnore] // refresh token is returned in http only cookie
    //    //public string RefreshToken { get; set; }

    //    public HardwareAuthenticationResponse(string jwtToken, string refreshToken)
    //    {
    //        JwtToken = jwtToken;
    //        RefreshToken = refreshToken;
    //    }
    //}

    //public class RevokeHardwareTokenRequest : BaseRevokeTokenRequest
    //{
    //    //public string Token { get; set; }
    //}

    ////public class RefreshHardwareToken
    ////{
    ////    public string? JwtToken { get; set; }
    ////    public string? RefreshToken { get; set; }
    ////}

    //public class RefreshHardwareToken : BaseRefreshLicenseToken
    //{
    //    //[Key]
    //    //[JsonIgnore]
    //    //public int Id { get; set; }

    //    //public string Token { get; set; }
    //    //public DateTime Expires { get; set; }
    //    //public bool IsExpired => DateTime.UtcNow >= Expires;
    //    //public DateTime Created { get; set; }
    //    //public string CreatedByIp { get; set; }
    //    //public DateTime? Revoked { get; set; }
    //    //public string RevokedByIp { get; set; }
    //    //public string ReplacedByToken { get; set; }
    //    //public bool IsActive => Revoked == null && !IsExpired;
    //}
}
