// SPDX-FileCopyrightText: 2026 Febris
// SPDX-License-Identifier: AGPL-3.0-only
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Febris.SharedMobileLibrary.Enums
{
    //################################################################
    //################################################################
    public enum XApiInteractionType
    {
        [Display(Name = "true-false")] true_false,
        [Display(Name = "choice")] choice,
        [Display(Name = "fill-in")] fill_in,
        [Display(Name = "long-fill-in")] long_fill_in,
        [Display(Name = "matching")] matching,
        [Display(Name = "performance")] performance,
        [Display(Name = "sequencing")] sequencing,
        [Display(Name = "likert")] likert,
        [Display(Name = "numeric")] numeric,
        [Display(Name = "other")] other,
    }
    public class InteractionTypeResolver
    {
        public static string XApiInteractionTypeResolver(XApiInteractionType input)
        {
            string output = string.Empty;
            switch (input)
            {
                case XApiInteractionType.true_false:
                    output = "[true,false]";
                    break;
                case XApiInteractionType.choice:
                    output = "[,]";
                    break;
                case XApiInteractionType.fill_in:
                    output = "[,]";
                    break;
                case XApiInteractionType.long_fill_in:
                    output = "[,]";
                    break;
                case XApiInteractionType.matching:
                    output = "[,]";
                    break;
                case XApiInteractionType.performance:
                    output = "[,]";
                    break;
                case XApiInteractionType.sequencing:
                    output = "[,]";
                    break;
                case XApiInteractionType.likert:
                    output = "id";
                    break;
                case XApiInteractionType.numeric:
                    output = "[:]";
                    break;
                case XApiInteractionType.other:
                    output = "string";
                    break;
            }
            return output;
        }
    }
}
