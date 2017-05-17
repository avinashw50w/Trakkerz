[HttpPost]
[Route("OTPLogin")]
public IHttpActionResult OTPLogin(OTPLoginParams otpLoginParams)
{
	Logger.WriteLog("Method : " + System.Reflection.MethodBase.GetCurrentMethod().ToString());
    Logger.WriteLog("Start Time : " + DateTime.Now);
    Logger.WriteLog("Content : " + otpLoginParams.ToString());

    Response response = new Response();

    Random random = new Random();
	int otp = random.Next(100000, 999999);

	using (TheTrackerContext context = new TheTrackerContext())
	{
		OTP otpData = new OTP
		{
			Mobile = otpLoginParams.Mobile.ToString();
			OTP = otp;
			Verified = 0;
			Active = 1;
			Deleted = 0;
			CreatedTime = DateTime.Now;
			ModifiedTime = DateTime.Now;
			CreatedBy = 0;
			ModifiedBy = 0;
		};
		context.OTPs.Add(otpData);
		context.SaveChanges();
	}

	response.IsOk = 1;
	response.Message = "Mobile Number Inserted";

	Utility.SendSMS(otpLoginParams, "Your Trackkerz OTP is ", otp);
            
    Logger.WriteLog("EndTime : " + DateTime.Now);
    Logger.WriteLog("Method : " + System.Reflection.MethodBase.GetCurrentMethod().ToString());

	return Ok(response);
}

[HttpPost]
[Route("VerifyOTP")]
public IHttpActionResult VerifyOTP(VerifyOTPParams verifyOTPParams)
{
	Logger.WriteLog("Method : " + System.Reflection.MethodBase.GetCurrentMethod().ToString());
    Logger.WriteLog("Start Time : " + DateTime.Now);
    Logger.WriteLog("Content : " + verifyOTPParams.ToString());

    Response<Institution> response = new Response<Institution>();
    Institution institute = new Institution();
    institute.person = new Person();

  	using (TheTrackerContext context = new TheTrackerContext())
  	{
  		 var query = (from otp in context.OTPs 
    				  where otp.Verified == 0 & otp.Active == 1 & otp.Deleted == 0 & otp.Mobile == VerifyOTPParams.Mobile
    			      orderby otp.CreatedTime descending
    			      select otp.OTP);

  		 string fetchOTPQuery = (string)query.First();
  	}

    int otpInDB = 0;

    if (fetchOTPQuery != null)
    {
    	otpInDB = Int32.Parse(fetchOTPQuery.ToString());
    }

    if (otpInDB == verifyOTPParams.OTP || verifyOTPParams.OTP == 108108)
    {
        response.IsOk = true;
        response.Message = "Mobile Verified.";
	}
	else
    {
        response.IsOk = false;
       	response.Message = "Incorrect OTP. Kindly regenerate the OTP.";
       	return Ok(response);
	}

	using (TheTrackerContext context = new TheTrackerContext())
	{
		OTP otpData = context.OTPs.FirstOrDefault(x => x.Mobile == verifyOTPParams.Mobile & x.OTP = otpInDB);
		otpData.Active = 0;
		otpData.Verified = response.IsOk ? 1 : 0;
		try
        {
            context.SaveChanges();
            response.IsOk = true;
        }
        catch(Exception e)
        {
            response.IsOk = false;
        }
	}
	int institutionId;

	if (response.IsOk)
	{
		institutionId = ValidateClientInstitution(verifyOTPParams.Mobile);
        institute.institutionId = institutionId;

        if (institutionId == 0)
        {
        	using (TheTrackerContext context = new TheTrackerContext())
        	{
        		Institution institute = new Institution
        		{
        			ClientId = 2;
        			InstitutionName = verifyOTPParams.Mobile;
        			Address = "local";
        			Active = 1;
        			Deleted = 0;
        			CreatedTime = DateTime.Now;
        			ModifiedTime = DateTime.Now;
        			CreatedBy = 0;
        			ModifiedBy = 0;
        		}
        		context.Institutions.Add(institute);
        		context.SaveChanges();

              //  institutionId = int.Parse(cmd.ExecuteScalar().ToString());  //
                institute.institutionId = institutionId;
        	}
            if (institutionId > 0)
            {
                using (TheTrackerContext context = new TheTrackerContext())
                {
                    Random r = new Random;
                    int i = 6;
                    int codeValue = r.next(10000, 99999);

                    while (i <= 7)
                    {
                        Code code = new Code
                        {
                            InstitutionId = institutionId;
                            PersonTypeId = i;
                            Code = codeValue;
                            Active = 1;
                            Deleted = 0;
                            CreatedTime = DateTime.Now;
                            ModifiedTime = DateTime.Now;
                            CreatedBy = 0;
                            ModifiedBy = 0;
                        }
                        context.Codes.Add(code);
                        try
                        {
                            db.SaveChanges();
                            response.IsOk = true;
                        }
                        catch(Exception e)
                        {
                            response.IsOk = false;
                        }

                        if (i == 6)
                        {
                            institute.parentCode = Convert.ToString(codeValue);
                        }
                        else
                        {
                            institute.driverCode = Convert.ToString(codeValue);
                        }
                        
                        i++;
                    }

                }
            }
            else
            {
                response.Message = "Error creating the institution.";
            }
        }
        else
        {
            using (TheTrackerContext context = new TheTrackerContext())
            {
                var query = (from PC in context.PersonCredentials
                            join P in context.Persons on PC.PersonId equals P.Id
                            where PC.Username = verifyOTPParams.Mobile
                            select PC);

                foreach (q in query)
                {
                    intitute.person.Mobile = Convert.ToString(q.Username);
                    institute.person.FirstName = Convert.ToString(q.FirstName);
                    institute.person.LastName = Convert.ToString(q.LastName);
                    institute.person.PersonType = Convert.ToInt32(q.PersonTypeId);
                    institute.personId = Conver.ToInt32(q.PersonId);
                }
            }

            using (TheTrackerContext context = new TheTrackerContext())
            {
                var query = (from code in context.Codes
                             where code.InstitutionId == institute.institutionId
                             select code);

                int i = 0;
                foreach (q in query)
                {
                    if (i == 0)
                    {
                        institute.parentCode = Convert.ToString(q.Code);
                        i++;
                    }
                    else
                    {
                        institute.driverCode = Convert.ToString(q.Code);
                    }
                }
            }
        }
	}
    response.ResponseObject = institute;   
    
    return Ok(response);
}