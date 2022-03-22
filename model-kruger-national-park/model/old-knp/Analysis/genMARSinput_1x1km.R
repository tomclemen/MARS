  d <- read.table("~/ownCloud/MARS_Modelle/MARS_KNP/RCP Daten/veg_knp_wifi_RCP45.dat")
  
  xllcorner    <- 30.5
  yllcorner    <- -25.50
  ncols        <- 200
  nrows        <- 400
  cellsize     <- 0.008
  nodata_value <- -9999
  
  xseq <- seq( xllcorner, length=ncols, by=cellsize )
  yseq <- seq( yllcorner, length=nrows, by=cellsize )
  
  # sort data file in respect to x and y coordinate
  xx <- sort(unique(d[,1]))
  yy <- rev(sort(unique(d[,2])))
  
  yr_start <- 2031
  yr_end <- 2031
  
  for ( tt in c(yr_start:yr_end) )
  {
    # initialize result matrices with NA
    lgb <- matrix( NA, length(yseq), length(xseq) )  # live grass biomass
    mx  <- matrix( NA, length(yseq), length(xseq) )
    my  <- matrix( NA, length(yseq), length(xseq) )
    
    for ( xorig in 1:length(xx))
    {
      for ( yorig in 1:length(yy) )
      {
        b <- subset( d, d[,1]==xx[xorig] & d[,2]==yy[yorig] & d[,3]==tt )
        
        # fill 50x50 cells of target matrix with original value scaled to km^2
        
        for ( x in (1+(xorig-1)*50):(xorig*50) )
        {
          for ( y in (1+(yorig-1)*50):(yorig*50) )
          {
            lgb[y,x]<-b[,4] * 100
          }
        }
      }
    }
  
  
    # create .asc file
  
    filename <- paste("~/Downloads/",
                    tt,".asc", sep ="")
    sink(filename)
    cat("ncols 200",sep="\n")
    cat("nrows 400",sep="\n")
    cat("xllcorner 30.5",sep="\n")
    cat("yllcorner -25.50",sep="\n")
    cat("cellsize 0.008",sep="\n")
    cat("nodata_value -9999",sep="\n")
    for ( y in 1:length(yseq))
    {
      cat(lgb[y,])
      cat("\n")
    }
    sink()
  }
  
  # meta_file <- "~/Downloads/metadata.csv"
  # sink(meta_file)
  # cat("Date;File", sep = "\n")
  # 
  # for ( tt in c(yr_start:yr_end) )
  # {
  #     cat(paste(tt,"-01-01T00:00:00;",tt,".asc",sep = ""))
  #     cat("\n")
  # }
  
  #sink()
  
