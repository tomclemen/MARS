library(ncdf4)
library(raster)
library(maptools)
library(rgdal)
library(plyr)
library(RColorBrewer)

library(ggplot2)
library(ggspatial)

data(wrld_simpl)

borders <- shapefile("./gis_raster_border/gis_raster_border.shp", stringsAsFactors=FALSE, verbose=FALSE, warnPRJ=TRUE)
##-----------------------------------------------------------------------
# open file
d <- read.table("./Griddata-CSV.dat")
##-----------------------------------------------------------------------
# read fire and aboveground biomass from pop file

# sort data file in respect to x and y coordinate
xx <- sort(unique(d[,1]))
yy <- rev(sort(unique(d[,2])))

# no output on screen
# graphics.off()

# output to png file
png( file="./griddata-1980.png", height=825, width=595 )

# format output frame (4 cols, smaller inner and outer margin)
par( mfrow=c(1,1), mar=c(3,3,1,4), oma=c(0,2,2,1) )

# select years -> tt
for ( tt in c(1980) )
{
  # initialize result matrices with NA
  aglb1 <- matrix( NA, length(yy), length(xx) )
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
      h <- mean(b[,4], na.rm=T)
      
      aglb1[y,x] <- h
      mx[y,x]<- xx[x]
      my[y,x]<- yy[y]
    }
  }
  print(xx)
  print(yy)
  lonlat     <- expand.grid(xx+0.25,yy-0.25)
  
  xyz_aglb1   <- data.frame(cbind(lonlat,as.vector(t(aglb1))))
  xyz_aglb1_r <- rasterFromXYZ(xyz_aglb1, crs=4326, digits=5)
  raster

  # ---------------------------------------------------------------------
  # --- Plots -----------------------------------------------------------
  # ---------------------------------------------------------------------
  
  #bm_brk <- seq(0,max(d[,4]), by=100)
  bm_brk <- seq(0,270, by=40)
  #bm_brk <- seq(0,90, by=12)
  bm_col <- brewer.pal(length(bm_brk),"Greens")
  
  
  plot(xyz_aglb1_r, col=bm_col, breaks=bm_brk, main=tt )
  plot(borders, add=T)
  plot(wrld_simpl,add=T)
  
}
#graphics.off()