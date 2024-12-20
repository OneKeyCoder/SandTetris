﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SandTetris.Entities;

public class Department
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<Employee> Employees { get; set; } = [];
    public string? HeadOfDepartmentId { get; set; }
    public Employee? HeadOfDepartment { get; set; }
}
