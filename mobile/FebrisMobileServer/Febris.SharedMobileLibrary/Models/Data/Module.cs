// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using Febris.SharedMobileLibrary.Enums;
using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Febris.SharedMobileLibrary.Models.Data
{
    public class Module : BaseModel
    {
        public Module()
        {
            UUID = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow.ToString("s");
            LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
        }

        public Module(ModuleStorage input)
        {
            if (input.Id != default)
            {
                Id = input.Id;
            }
            if (input.UUID != default)
            {
                UUID = input.UUID;
            }
            else
            {
                UUID = Guid.NewGuid();
            }
            if (input.TimeStamp != default)
            {
                TimeStamp = input.TimeStamp;
            }
            else
            {
                TimeStamp = DateTime.UtcNow.ToString("s");
            }
            if (input.LastUpdateTimeStamp != default)
            {
                LastUpdateTimeStamp = input.LastUpdateTimeStamp;
            }
            else
            {
                LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            }
            Obsolete = input.Obsolete;
            Name = input.Name;
            Version = input.Version;
            Description = input.Description;
            //ModuleClassification = input.ModuleClassification?.Id ?? default;
            //ModuleClassificationUUID = input.ModuleClassification?.UUID ?? default;
            Language = input.Language;
            XApiInteractionType = input.XApiInteractionType;
            MainSectionCount = input.MainSectionCount;
            TotalSectionCount = input.TotalSectionCount;
            InteractionComponents = input.InteractionComponents;
            EstimatedCompletionTime = input.EstimatedCompletionTime;
        }
        public Module(ModuleStorage input, ModuleClassification child)
        {
            if (input.Id != default)
            {
                Id = input.Id;
            }
            if (input.UUID != default)
            {
                UUID = input.UUID;
            }
            else
            {
                UUID = Guid.NewGuid();
            }
            if (input.TimeStamp != default)
            {
                TimeStamp = input.TimeStamp;
            }
            else
            {
                TimeStamp = DateTime.UtcNow.ToString("s");
            }
            if (input.LastUpdateTimeStamp != default)
            {
                LastUpdateTimeStamp = input.LastUpdateTimeStamp;
            }
            else
            {
                LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            }
            Obsolete = input.Obsolete;
            Name = input.Name;
            Version = input.Version;
            Description = input.Description;
            ModuleClassification = child;
            Language = input.Language;
            XApiInteractionType = input.XApiInteractionType;
            MainSectionCount = input.MainSectionCount;
            TotalSectionCount = input.TotalSectionCount;
            InteractionComponents = input.InteractionComponents;
            EstimatedCompletionTime = input.EstimatedCompletionTime;
        }


        //for db
        //public long Id { get; set; }
        //public Guid UUID { get; set; }


        //Out of date  
        //[Display(Name = "This Module is Obsolete")]
        public bool Obsolete { get; set; }

        //Basic information
        //[Display(Name = "Creation date")]
        //public DateTime CreationTimeStamp { get; set; }

        //[Display(Name = "Last Modified")]
        //public DateTime UpdateTimeStamp { get; set; }



        //[Display(Name = "Course Name")]
        public string Name { get; set; }

        public string Version { get; set; }

        // [Display(Name = "Description")]
        public string Description { get; set; }

        //[ForeignKey("Id")]        
        //public long ModuleClassificationId { get; set; }
        public ModuleClassification ModuleClassification { get; set; }
        public Guid ModuleClassificationUUID { get; set; }

        /// <summary>
        /// new changes for marketplace
        /// 
        /// mayybe this should not be here. Could move this to its own lookup model
        /// </summary>        
        //public decimal Price { get; set; }
        //[Display(Name = "Education Category")]
        //public Industry Industry { get; set; }
        //[Display(Name = "Most Relevant Field")]
        //public Category Category { get; set; }        


        //catagorizing
        //[Display(Name = "Education Category")]
        //public EducationCategory EducationCategory { get; set; }
        //[Display(Name = "Most Relevant Field")]
        //public FieldType FieldType { get; set; }

        // [Display(Name = "Pick this educaiton's main language (should be en-US)")]
        public LanguageMapTypeEnum Language { get; set; }

        // [Display(Name = "Pick Interaction type (should be Performance for VR)")]
        public XApiInteractionType XApiInteractionType { get; set; }

        //step information
        // [Display(Name = "Main section count")]
        public int MainSectionCount { get; set; }

        // [Display(Name = "All sections and subsection count")]
        public int TotalSectionCount { get; set; }

        // [Display(Name = "Solutions to test for steps to follow xAPI specificaiton")]
        public string InteractionComponents { get; set; }

        //[Display(Name = "Estimated Completion Time in minutes")]
        public int EstimatedCompletionTime { get; set; }

        //test vs education
        //[Display(Name = "This listing is a Test and not Training.")]
        //public bool IsTest { get; set; }




    }

    public class ModuleStorage : BaseModel
    {
        public ModuleStorage()
        {
            UUID = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow.ToString("s");
            LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
        }
        public ModuleStorage(Module input)
        {
            if (input.Id != default)
            {
                Id = input.Id;
            }
            if (input.UUID != default)
            {
                UUID = input.UUID;
            }
            else
            {
                UUID = Guid.NewGuid();
            }
            if (input.TimeStamp != default)
            {
                TimeStamp = input.TimeStamp;
            }
            else
            {
                TimeStamp = DateTime.UtcNow.ToString("s");
            }
            if (input.LastUpdateTimeStamp != default)
            {
                LastUpdateTimeStamp = input.LastUpdateTimeStamp;
            }
            else
            {
                LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            }
            Obsolete = input.Obsolete;
            Name = input.Name;
            Version = input.Version ?? default;
            Description = input.Description ?? default;
            ModuleClassificationId = input.ModuleClassification?.Id ?? default;
            ModuleClassificationUUID = input.ModuleClassification?.UUID ?? default;
            Language = input.Language;
            XApiInteractionType = input.XApiInteractionType;
            MainSectionCount = input.MainSectionCount;
            TotalSectionCount = input.TotalSectionCount;
            InteractionComponents = input.InteractionComponents ?? default;
            EstimatedCompletionTime = input.EstimatedCompletionTime;
        }

        public bool Obsolete { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public int MainSectionCount { get; set; }
        public int TotalSectionCount { get; set; }
        public string InteractionComponents { get; set; }
        public int EstimatedCompletionTime { get; set; }

        ///references
        [ForeignKey("ModuleClassification")]
        public long ModuleClassificationId { get; set; }
        public Guid ModuleClassificationUUID { get; set; }
        public LanguageMapTypeEnum Language { get; set; }
        public XApiInteractionType XApiInteractionType { get; set; }
    }
    public class ModuleClassification : BaseModel
    {

        public bool Obsolete { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ModuleLinkedClassification : BaseModel
    {


        public ModuleClassification ModuleClassification { get; set; }
        public Guid ModuleClassificationUUID { get; set; }

        public Module Module { get; set; }
        public Guid ModuleUUID { get; set; }

    }

    public class ModuleLinkedCurriculum : BaseModel
    {

        public Guid CurriculumUUID { get; set; }
        public Curriculum Curriculum { get; set; }
        public Guid ModuleUUID { get; set; }
        public Module Module { get; set; }
    }

    public class Curriculum : BaseModel
    {
        public bool Obsolete { get; set; }

        public CurriculumClassification CurriculumClassification { get; set; }
        public Guid? CurriculumClassificationUUID { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }

    }

    public class CurriculumClassification : BaseModel
    {
        //public long Id { get; set; }
        //public Guid UUID { get; set; }
        //[Display(Name = "Creation date")]
        //public DateTime CreationTimeStamp { get; set; }

        //[Display(Name = "Last Modified")]
        //public DateTime UpdateTimeStamp { get; set; }

        public bool Obsolete { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class ModuleFileModel : BaseModel
    {
        public ModuleFileModel(ModuleFileModel input)
        {

            if (input.Id != default)
            {
                Id = input.Id;
            }
            if (input.UUID != default)
            {
                UUID = input.UUID;
            }
            else
            {
                UUID = Guid.NewGuid();
            }
            if (input.TimeStamp != default)
            {
                TimeStamp = input.TimeStamp;
            }
            else
            {
                TimeStamp = DateTime.UtcNow.ToString("s");
            }
            if (input.LastUpdateTimeStamp != default)
            {
                LastUpdateTimeStamp = input.LastUpdateTimeStamp;
            }
            else
            {
                LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            }
            FilePath = input.FilePath;
            Compressed = input.Compressed;
            ModuleDirectoryName = input.ModuleDirectoryName;

        }
        public ModuleFileModel()
        {
            UUID = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow.ToString("s");
            LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
        }
        //public Module Module { get; set; }
        public string ModuleDirectoryName { get; set; }
        public string FilePath { get; set; }
        public bool Compressed { get; set; }
        //public bool Installed { get; set; }
    }
    /// <summary>
    /// This may need some fine tuning
    /// </summary>
    public class ModulePackageModel : BaseModel
    {
        public ModulePackageModel()
        {
            UUID = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow.ToString("s");
            LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
        }
        public ModulePackageModel(ModulePackageStorageModel input)
        {
            if (input.Id != default)
            {
                Id = input.Id;
            }
            if (input.UUID != default)
            {
                UUID = input.UUID;
            }
            else
            {
                UUID = Guid.NewGuid();
            }
            if (input.TimeStamp != default)
            {
                TimeStamp = input.TimeStamp;
            }
            else
            {
                TimeStamp = DateTime.UtcNow.ToString("s");
            }
            if (input.LastUpdateTimeStamp != default)
            {
                LastUpdateTimeStamp = input.LastUpdateTimeStamp;
            }
            else
            {
                LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            }
            UriString = input.UriString;
            Installed = input.Installed;
            ModuleDirectoryName = input.ModuleDirectoryName;
            if (input.Name != default)
            {
                Name = input.Name;
            }

        }
        public ModulePackageModel(ModulePackageStorageModel input, Module chile1, ModuleFileModel decompressedModuleFile, ModuleFileModel compressedModuleFile)
        {
            if (input.Id != default)
            {
                Id = input.Id;
            }
            if (input.UUID != default)
            {
                UUID = input.UUID;
            }
            else
            {
                UUID = Guid.NewGuid();
            }
            if (input.TimeStamp != default)
            {
                TimeStamp = input.TimeStamp;
            }
            else
            {
                TimeStamp = DateTime.UtcNow.ToString("s");
            }
            if (input.LastUpdateTimeStamp != default)
            {
                LastUpdateTimeStamp = input.LastUpdateTimeStamp;
            }
            else
            {
                LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            }
            Module = chile1;
            ModuleFile = decompressedModuleFile;
            CompressedModuleFile = compressedModuleFile;
            UriString = input.UriString;
            Installed = input.Installed;
            ModuleDirectoryName = input.ModuleDirectoryName;
            if (input.Name != default)
            {
                Name = input.Name;
            }
        }
        public ModulePackageModel(ModulePackageStorageModel input, Module chile1)
        {
            if (input.Id != default)
            {
                Id = input.Id;
            }
            if (input.UUID != default)
            {
                UUID = input.UUID;
            }
            else
            {
                UUID = Guid.NewGuid();
            }
            if (input.TimeStamp != default)
            {
                TimeStamp = input.TimeStamp;
            }
            else
            {
                TimeStamp = DateTime.UtcNow.ToString("s");
            }
            if (input.LastUpdateTimeStamp != default)
            {
                LastUpdateTimeStamp = input.LastUpdateTimeStamp;
            }
            else
            {
                LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            }
            Module = chile1;
            UriString = input.UriString;
            Installed = input.Installed;
            ModuleDirectoryName = input.ModuleDirectoryName;
            if (input.Name != default)
            {
                Name = input.Name;
            }
        }
        public Module Module { get; set; }
        public ModuleFileModel ModuleFile { get; set; }
        public ModuleFileModel CompressedModuleFile { get; set; }
        //public string LocationPath { get; set; }
        public string UriString { get; set; }
        public bool Installed { get; set; }
        public string ModuleDirectoryName { get; set; }

        /// <summary>
        /// This property was previously missused. Only set when PackageInfo is used
        /// </summary>
        public string Name { get; set; }
    }

    public class ModulePackageStorageModel : BaseModel
    {
        public ModulePackageStorageModel()
        {
            UUID = Guid.NewGuid();
            TimeStamp = DateTime.UtcNow.ToString("s");
            LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
        }

        public ModulePackageStorageModel(ModulePackageModel input)
        {



            if (input.Id != default)
            {
                Id = input.Id;
            }
            if (input.UUID != default)
            {
                UUID = input.UUID;
            }
            else
            {
                UUID = Guid.NewGuid();
            }
            if (input.TimeStamp != default)
            {
                TimeStamp = input.TimeStamp;
            }
            else
            {
                TimeStamp = DateTime.UtcNow.ToString("s");
            }
            if (input.LastUpdateTimeStamp != default)
            {
                LastUpdateTimeStamp = input.LastUpdateTimeStamp;
            }
            else
            {
                LastUpdateTimeStamp = DateTime.UtcNow.ToString("s");
            }
            ModuleId = input.Module?.Id ?? default;
            ModuleUUID = input.Module?.UUID ?? default;
            ModuleFileId = input.ModuleFile?.Id ?? default;
            ModuleFileUUID = input.ModuleFile?.UUID ?? default;
            CompressedModuleFileId = input.CompressedModuleFile?.Id ?? default;
            CompressedModuleFileUUID = input.CompressedModuleFile?.UUID ?? default;
            UriString = input.UriString;
            Installed = input.Installed;
            ModuleDirectoryName = input.ModuleDirectoryName;
            if (input.Name != default)
            {
                Name = input.Name;
            }

        }


        //[ForeignKey("Id")]
        //[Association(TableName = "Model")]

        //public ModuleFileModel ModuleFile { get; set; }
        //public string LocationPath { get; set; }
        public string UriString { get; set; }
        public bool Installed { get; set; }

        /// <summary>
        /// This property was missused previously. It is the actual packagename found through PackageInfo and handed back. Do not set this after install
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// This was poorly named before. Now named more directly.
        /// </summary>
        public string ModuleDirectoryName { get; set; }



        ///References        
        [ForeignKey("ModuleStorage"), Required]
        public long ModuleId { get; set; }
        public Guid ModuleUUID { get; set; }

        [ForeignKey("ModuleFileModel"), Required]
        public long ModuleFileId { get; set; }
        public Guid ModuleFileUUID { get; set; }

        [ForeignKey("ModuleFileModel"), Required]
        public long CompressedModuleFileId { get; set; }
        public Guid CompressedModuleFileUUID { get; set; }
    }
}
