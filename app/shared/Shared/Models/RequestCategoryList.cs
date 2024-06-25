// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Models;
public class RequestCategoryList
{
    public RequestCategoryList()
    {

    }
    public CategoryList CategoryList { get; set; } = new();

    public void AddCategory(string category)
    {
        
        if (!CategoryList.Categories.Contains(category))
        {
            CategoryList.Categories.Add(category);
        }
    }

    public List<string> GetCategories()
    {
        return CategoryList.Categories;
    }

    public void SaveCategories()
    {
        Console.WriteLine("Saving categories to file");

        // Assuming the base directory is the root of the project
        string relativePath = "../../../../../../shared/Shared/Data/categories.txt";
        //string filePath = Path.Combine(baseDirectory, relativePath);

        Console.WriteLine($"File path: {relativePath}");

        // Ensure the directory exists
        string directoryPath = Path.GetDirectoryName(relativePath);
        if (!Directory.Exists(directoryPath))
        {
            Console.WriteLine($"Creating directory: {directoryPath}");
            Directory.CreateDirectory(directoryPath);
        }

        Console.WriteLine(String.Join(",", CategoryList.Categories));

        // Write all lines to the file
        File.WriteAllText(relativePath, String.Join(",", CategoryList.Categories));
        File.WriteAllText(relativePath,"test");
    }

}
