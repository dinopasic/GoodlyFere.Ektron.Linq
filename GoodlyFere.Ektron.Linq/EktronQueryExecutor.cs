﻿#region License

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EktronQueryExecutor.cs">
// LINQ to Ektron Search, a LINQ interface to the Ektron AdvancedSearchCriteria search engine
// 
// Copyright (C) 2013 Benjamin Ramey
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// 
// http://www.gnu.org/licenses/lgpl-2.1-standalone.html
// 
// You can contact me at ben.ramey@gmail.com.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

#endregion

#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Ektron.Cms.Search;
using GoodlyFere.Ektron.Linq.Exceptions;
using GoodlyFere.Ektron.Linq.Execution.Maps;
using GoodlyFere.Ektron.Linq.Execution.Search;
using GoodlyFere.Ektron.Linq.Generation;
using GoodlyFere.Ektron.Linq.Helpers;
using GoodlyFere.Ektron.Linq.Interfaces;
using Remotion.Linq;
using Ek = Ektron.Cms.Search.Expressions;

#endregion

namespace GoodlyFere.Ektron.Linq
{
    public class EktronQueryExecutor : IQueryExecutor
    {
        #region Constants and Fields

        private static readonly ILog Log = LogManager.GetLogger<EktronQueryExecutor>();

        private readonly IEktronIdProvider _idProvider;
        private readonly ScalarResultMaps _scalarResultMappings;
        private readonly EktronSearcher _searcher;

        #endregion

        #region Constructors and Destructors

        public EktronQueryExecutor(IEktronIdProvider idProvider, ISearchManager searchManager)
        {
            _idProvider = idProvider;
            _searcher = new EktronSearcher(searchManager);
            _scalarResultMappings = new ScalarResultMaps();
        }

        #endregion

        #region Public Methods

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var criteria = CreateCriteria<T>(queryModel, 10000);

            List<SearchResultData> results = _searcher.DoSearch(criteria);
            var converter = new ResultsConverter<T>();
            return converter.GetMany(results, criteria);
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            if (!_scalarResultMappings.ContainsKey(typeof(T)))
            {
                throw new InvalidQueryException(
                    string.Format("'{0}' result operators are not supported.", typeof(T).Name));
            }

            AdvancedSearchCriteria criteria = CreateScalarCriteria(queryModel);
            IDictionary handlerMap = _scalarResultMappings[typeof(T)];
            T returnValue = default(T);

            foreach (var resultOperator in queryModel.ResultOperators)
            {
                if (!handlerMap.Contains(resultOperator.GetType()))
                {
                    throw new InvalidQueryException(
                        string.Format("'{0}' result operator is not supported.", resultOperator.GetType().Name));
                }

                var handler = (ScalarResultHandlerMethod<T>)handlerMap[resultOperator.GetType()];
                returnValue = handler.Invoke(criteria, _searcher);
            }

            return returnValue;
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var criteria = CreateCriteria<T>(queryModel, 1);

            List<SearchResultData> results = _searcher.DoSearch(criteria);
            var converter = new ResultsConverter<T>();

            T result = converter.GetSingle(results, criteria);
            if (result == null && !returnDefaultWhenEmpty)
            {
                throw new InvalidOperationException("The input sequence is empty.");
            }

            return result;
        }

        #endregion

        #region Methods

        protected AdvancedSearchCriteria CreateCriteria<T>(QueryModel queryModel, int recordsPerPage)
        {
            AdvancedSearchCriteria criteria = CriteriaGenerator.Generate(queryModel, _idProvider);
            criteria.PagingInfo.RecordsPerPage = recordsPerPage;
            criteria.Permission = Permission.CreateAdministratorPermission();
            criteria.ReturnProperties.UnionWith(PropertyExpressionHelper.GetPropertyExpressionsForType(typeof(T)));
            return criteria;
        }

        protected AdvancedSearchCriteria CreateScalarCriteria(QueryModel queryModel)
        {
            AdvancedSearchCriteria criteria = CriteriaGenerator.Generate(queryModel, _idProvider);
            criteria.Permission = Permission.CreateAdministratorPermission();
            criteria.ReturnProperties.Add(SearchContentProperty.Id);

            return criteria;
        }

        #endregion
    }
}