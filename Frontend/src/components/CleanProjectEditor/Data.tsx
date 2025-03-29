export const columns = [
    { columnKey: "companyName", label: "Company Name" },
    { columnKey: "industry", label: "Industry" },
    { columnKey: "revenue", label: "Revenue (USD Millions)" },
    { columnKey: "totalEmployees", label: "Employees" },
    { columnKey: "headquarters", label: "Headquarters" },
  ];

  export type DataItem = {
    companyName?: string;
    industry?: string;
    revenue?: string | number;
    totalEmployees?: string | number;
    headquarters?: string;
  };
export const corruptItems:DataItem[]  = [
    {
      "companyName": "Walmart",
      "industry": "Retail",
      "revenue": 648125,
      "totalEmployees": "None",
      "headquarters": "Bentonville, Arkansas"
    },
    {
      "companyName": "Amazon",
      "industry": "Retal",
      "revenue": 574785,
      "totalEmployees": 1525000,
      "headquarters": "Seattle, Washington"
    },
    {
      "companyName": "Apple",
      "industry": "Electronics",
      "revenue": 383482,
      "totalEmployees": 161000,
      "headquarters": "Cupertino, California"
    },
    {
      "companyName": "UnitedHealth Group",
      "industry": "Healthcare",
      "revenue": "N/A",
      "totalEmployees": "Cats",
      "headquarters": "Minnetonka, Minnesota"
    },
    {
      "companyName": "Berkshire Hathaway",
      "industry": "Conglomerate",
      "revenue": 364482,
      "totalEmployees": 396500,
      "headquarters": "Omaha, Nebraska"
    },
    {
      "companyName": "CVS Health",
      "industry": "Healthcare",
      "revenue": 357776,
      "totalEmployees": 259500,
      "headquarters": "Woonsocket, Rhode Island"
    },
    {
      "companyName": "ExxonMobil",
      "industry": "Spongebob",
      "revenue": "-",
      "totalEmployees": "NaN",
      "headquarters": ""
    },
    {
      "companyName": "Alphabet",
      "industry": "Heathcare",
      "revenue": "N/A",
      "totalEmployees": "??",
      "headquarters": "Mountain View, California"
    },
    {
      "companyName": "McKesson Corporation",
      "industry": "Healthcare",
      "revenue": "$500M",
      "totalEmployees": "??",
      "headquarters": "Irving, Texas"
    },
    {
      "companyName": "Cencora",
      "industry": "Pharmaceuticals",
      "revenue": 262173,
      "totalEmployees": 21400,
      "headquarters": "Conshohocken, Pennsylvania"
    }
  ];
export const cleanItems:DataItem[] = [
    {
      "companyName": "Walmart",
      "industry": "Retail",
      "revenue": 648125,
      "totalEmployees": 2100000,
      "headquarters": "Bentonville, Arkansas"
    },
    {
      "companyName": "Amazon",
      "industry": "Retail and Cloud Computing",
      "revenue": 574785,
      "totalEmployees": 1525000,
      "headquarters": "Seattle, Washington"
    },
    {
      "companyName": "Apple",
      "industry": "Electronics",
      "revenue": 383482,
      "totalEmployees": 161000,
      "headquarters": "Cupertino, California"
    },
    {
      "companyName": "UnitedHealth Group",
      "industry": "Healthcare",
      "revenue": 371622,
      "totalEmployees": 440000,
      "headquarters": "Minnetonka, Minnesota"
    },
    {
      "companyName": "Berkshire Hathaway",
      "industry": "Conglomerate",
      "revenue": 364482,
      "totalEmployees": 396500,
      "headquarters": "Omaha, Nebraska"
    },
    {
      "companyName": "CVS Health",
      "industry": "Healthcare",
      "revenue": 357776,
      "totalEmployees": 259500,
      "headquarters": "Woonsocket, Rhode Island"
    },
    {
      "companyName": "ExxonMobil",
      "industry": "Petroleum",
      "revenue": 344582,
      "totalEmployees": 61500,
      "headquarters": "Spring, Texas"
    },
    {
      "companyName": "Alphabet",
      "industry": "Technology and Cloud Computing",
      "revenue": 307394,
      "totalEmployees": 182502,
      "headquarters": "Mountain View, California"
    },
    {
      "companyName": "McKesson Corporation",
      "industry": "Healthcare",
      "revenue": 276711,
      "totalEmployees": 48000,
      "headquarters": "Irving, Texas"
    },
    {
      "companyName": "Cencora",
      "industry": "Pharmaceuticals",
      "revenue": 262173,
      "totalEmployees": 21400,
      "headquarters": "Conshohocken, Pennsylvania"
    }
  ];