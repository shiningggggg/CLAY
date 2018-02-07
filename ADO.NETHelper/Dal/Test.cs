using ADO.NETHelper.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ADO.NETHelper.Dal
{
    public class Test
    {
        public void Run()
        {
            #region 添加
            if (false)
            {
                OracleHelper.BeginTransaction();
                try
                {
                    //子表的子表
                    BrandChildChild bcc1 = new BrandChildChild();
                    bcc1.Id = BrandDal.GetNextID();
                    bcc1.Name = "bcc1" + DateTime.Now.ToString();
                    BrandDal.Insert(bcc1);

                    //子表
                    BrandChild bc1 = new BrandChild();
                    bc1.Id = BrandDal.GetNextID();
                    bc1.Name = "bc1" + DateTime.Now.ToString();
                    bc1.bcc = bcc1;
                    OracleHelper.Insert(bc1);

                    //主表
                    Brand bd1 = new Brand();
                    bd1.Id = BrandDal.GetNextID();
                    bd1.Name = "bd1" + DateTime.Now.ToString();
                    bd1.bc = bc1;

                    OracleHelper.Insert(bd1);

                    OracleHelper.EndTransaction();
                }
                catch (Exception ex)
                {
                    OracleHelper.RollbackTransaction();
                }
            } 
            #endregion

            #region 修改
            if (false)
            {
                OracleHelper.BeginTransaction();
                try
                {
                    //替换子表
                    BrandChild bc1 = new BrandChild();
                    bc1.Id = BrandDal.GetNextID();
                    bc1.Name = "bc1" + DateTime.Now.ToString();
                    OracleHelper.Insert(bc1);

                    //更新主表
                    Brand bd1 = OracleHelper.FindById<Brand>(27);
                    bd1.Name = "bd1" + DateTime.Now.ToString();
                    bd1.bc = bc1;

                    OracleHelper.Update(bd1);

                    OracleHelper.EndTransaction();
                }
                catch (Exception ex)
                {
                    OracleHelper.RollbackTransaction();
                }
            }
            #endregion

            #region 删除
            if (false)
            {
                OracleHelper.BeginTransaction();
                try
                {
                    OracleHelper.Delete<Brand>(21);
                    OracleHelper.BatchDelete<Brand>("22,23");

                    OracleHelper.EndTransaction();
                }
                catch (Exception ex)
                {
                    OracleHelper.RollbackTransaction();
                }
            }
            #endregion

            #region 大文本
            if (false)
            {
                Brand bd1 = OracleHelper.FindById<Brand>(27);
                bd1.Blb = Encoding.Default.GetBytes("大家好！");
                bd1.Clb = "大家好！";
                OracleHelper.Update(bd1);

                Brand bd12 = OracleHelper.FindById<Brand>(27);
            }
            #endregion

            #region 查询列表
            List<Brand> brandList = BrandDal.FindListBySql<Brand>("select * from BRAND");
            #endregion

            #region 分页查询列表
            PageViewModel pageViewModel = BrandDal.FindPageBySql<Brand>("select * from BRAND", "order by Id desc", 10, 1);
            #endregion
        }
    }
}
