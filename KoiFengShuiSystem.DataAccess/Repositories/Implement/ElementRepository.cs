﻿using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class ElementRepository : GenericRepository<Element>
    {
        public ElementRepository() { }
        public ElementRepository(KoiFengShuiContext context) => _context = context;
    }
}
