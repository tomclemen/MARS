d <- read.table("/Users/thc/ownCloud/2018 03 Land/RCP Daten/veg_knp_withfire_RCP85_now.dat")

# sort data file in respect to x and y coordinate
xx <- sort(unique(d[,1]))
yy <- rev(sort(unique(d[,2])))

for ( tt in c(1989:2003) )
{
  # initialize result matrices with NA
  lgb <- matrix( NA, length(yy), length(xx) )  # live grass biomass
  mx  <- matrix( NA, length(yy), length(xx) )
  my  <- matrix( NA, length(yy), length(xx) )
  
  # iterate by x,y coords and fill result matrices
  for ( x in 1:length(xx) )
  {
    for ( y in 1:length(yy) )
    {
      # select relevant rows from data file (x,y and year)
      b <- subset( d, d[,1]==xx[x] & d[,2]==yy[y] & d[,3]==tt )
      # select column to show (10=bio)
      #h <- mean(b[,4], na.rm=T)
      h <- b[,4]
      lgb[y,x] <- h * 250000 # km2 -> ha
      mx[y,x]<- xx[x]
      my[y,x]<- yy[y]
    }
  }


  # create .asc file

  filename <- paste("/Users/thc/ownCloud/2018 03 Land/MARS input/veg_knp_withfire_RCP85_now/",
                  tt,".asc", sep ="")
  sink(filename)
  cat("ncols 4",sep="\n")
  cat("nrows 8",sep="\n")
  cat("xllcorner 30.75",sep="\n")
  cat("yllcorner -25.25",sep="\n")
  cat("cellsize 0.5",sep="\n")
  cat("nodata_value -9999",sep="\n")
  for ( y in 1:length(yy))
  {
    cat(lgb[y,])
    cat("\n")
  }
  sink()
}
