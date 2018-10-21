<?xml version="1.0" encoding="utf-8"?>
<!--
  org.daisy.util (C) 2005-2008 Daisy Consortium
  
  This library is free software; you can redistribute it and/or modify it under
  the terms of the GNU Lesser General Public License as published by the Free
  Software Foundation; either version 2.1 of the License, or (at your option)
  any later version.
  
  This library is distributed in the hope that it will be useful, but WITHOUT
  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
  FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more
  details.
  
  You should have received a copy of the GNU Lesser General Public License
  along with this library; if not, write to the Free Software Foundation, Inc.,
  59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
--> 
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:dtb="http://www.daisy.org/z3986/2005/dtbook/" xmlns:s="http://www.w3.org/2001/SMIL20/"
	xmlns:m="http://www.w3.org/1998/Math/MathML" xmlns:svg="http://www.w3.org/2000/svg"
	xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns="http://www.w3.org/1999/xhtml"
	exclude-result-prefixes="dtb s m svg xs">

	<xsl:output 
		method="xhtml"
		encoding="utf-8" 
		indent="yes" 
		doctype-public="-//W3C//DTD XHTML 1.0 Transitional//EN" 
		doctype-system="http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dt"/>

	<!-- <!ENTITY catts "@id|@class|@title|@xml:lang"> -->
	<xsl:template name="copyCatts">
		<xsl:copy-of select="@id | @class | @title | @xml:lang"/>
	</xsl:template>

	<!-- <!ENTITY cncatts "@id|@title|@xml:lang"> -->
	<xsl:template name="copyCncatts">
		<xsl:copy-of select="@id | @title | @xml:lang"/>
	</xsl:template>

	<xsl:template name="copyAttsNoId">
		<xsl:copy-of select="@class | @title | @xml:lang"/>
	</xsl:template>

	<!-- <!ENTITY inlineParent "ancestor::*[self::dtb:h1 or self::dtb:h2 or self::dtb:h3 or self::dtb:h4 or self::dtb:h5 or self::dtb:h6 or self::dtb:hd or self::dtb:span or self::dtb:p]"> -->
	<xsl:template name="inlineParent">
		<xsl:param name="class"/>
		<xsl:choose>
			<xsl:when
				test="ancestor::*[self::dtb:h1 or self::dtb:h2 or self::dtb:h3 or self::dtb:h4 or self::dtb:h5 or self::dtb:h6 or self::dtb:hd or self::dtb:span or self::dtb:p or self::dtb:lic]">
				<xsl:apply-templates select="." mode="inlineOnly"/>
			</xsl:when>
			<!-- jpritchett@rfbd.org:  Fixed bug in setting @class value (missing braces) -->
			<xsl:otherwise>
				<div class="{$class}">
					<xsl:call-template name="copyCncatts"/>
					<xsl:apply-templates/>
				</div>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>


	<xsl:template match="dtb:dtbook">
		<xsl:element name="html" namespace="http://www.w3.org/1999/xhtml">
			<xsl:if test="@xml:lang">
				<xsl:copy-of select="@xml:lang"/>
			</xsl:if>
			<xsl:if test="@dir">
				<xsl:copy-of select="@dir"/>
			</xsl:if>
			<xsl:apply-templates/>
		</xsl:element>
	</xsl:template>


	<xsl:template match="dtb:head">
		<head>
			<meta http-equiv="Content-Type" content="application/xhtml+xml; charset=utf-8"/>
			<title>
				<xsl:value-of select="dtb:meta[@name = 'dc:Title']/@content"/>
			</title>
			<xsl:apply-templates/>
		</head>
	</xsl:template>


	<xsl:template match="dtb:meta">
		<meta>
			<xsl:if test="@name">
				<xsl:attribute name="name">
					<xsl:choose>
						<xsl:when test="@name = 'dtb:uid'">
							<xsl:value-of select="'dc:identifier'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Title'">
							<xsl:value-of select="'dc:title'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Subject'">
							<xsl:value-of select="'dc:subject'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Description'">
							<xsl:value-of select="'dc:description'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Type'">
							<xsl:value-of select="'dc:type'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Source'">
							<xsl:value-of select="'dc:source'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Relation'">
							<xsl:value-of select="'dc:relation'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Coverage'">
							<xsl:value-of select="'dc:coverage'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Creator'">
							<xsl:value-of select="'dc:creator'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Publisher'">
							<xsl:value-of select="'dc:publisher'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Contributor'">
							<xsl:value-of select="'dc:contributor'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Rights'">
							<xsl:value-of select="'dc:rights'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Date'">
							<xsl:value-of select="'dc:date'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Format'">
							<xsl:value-of select="'dc:format'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Identifier'">
							<xsl:value-of select="'dc:identifier'"/>
						</xsl:when>
						<xsl:when test="@name = 'dc:Language'">
							<xsl:value-of select="'dc:language'"/>
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="@name"/>
						</xsl:otherwise>
					</xsl:choose>
				</xsl:attribute>
			</xsl:if>
			<xsl:copy-of select="@http-equiv"/>
			<xsl:copy-of select="@content"/>
			<xsl:copy-of select="@scheme"/>
		</meta>
	</xsl:template>

	<!-- Unsure. How does this position for a copy? -->
	<xsl:template match="dtb:link">
		<!--
     <link>
       <xsl:copy-of select="@*"/>
     </link>
	-->
	</xsl:template>




	<xsl:template match="dtb:book">
		<body>
			<xsl:copy-of select="@xml:lang"/>
			<xsl:apply-templates/>
		</body>
	</xsl:template>

	<xsl:template match="dtb:frontmatter | dtb:bodymatter | dtb:rearmatter">
		<xsl:apply-templates/>
	</xsl:template>

	<xsl:template match="dtb:level1 | dtb:level2 | dtb:level3 | dtb:level4 | dtb:level5 | dtb:level6 | dtb:level">
		<div>
			<xsl:copy-of select="@xml:lang|@id|@class"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:covertitle">
		<p>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates mode="inlineOnly"/>
		</p>
	</xsl:template>



	<xsl:template match="dtb:p">
		<p><xsl:call-template name="copyCatts"/><xsl:apply-templates mode="inlineOnly"/></p>
	</xsl:template>


	<xsl:template name="pagenum">
		<span class="pagenum">
			<xsl:call-template name="copyCncatts"/>
			<xsl:choose>
				<xsl:when test="@page = 'front'">
					<xsl:attribute name="class">page-front</xsl:attribute>
				</xsl:when>
				<xsl:when test="@page = 'special'">
					<xsl:attribute name="class">page-special</xsl:attribute>
				</xsl:when>
				<xsl:otherwise>
					<xsl:attribute name="class">page-normal</xsl:attribute>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:apply-templates/>
			<!--<xsl:apply-templates/>-->
		</span>
	</xsl:template>

	<xsl:template match="dtb:pagenum">
		<xsl:call-template name="pagenum"/>
	</xsl:template>

	<xsl:template match="dtb:list/dtb:pagenum" priority="1">
		<xsl:param name="inlineFix"/>
		<xsl:choose>
			<xsl:when test="not(preceding-sibling::*) or $inlineFix = 'true'">
				<li>
					<xsl:call-template name="pagenum"/>
				</li>
			</xsl:when>
			<xsl:otherwise>
				<!--<xsl:message>Skipping pagenum element <xsl:value-of select="@id"/></xsl:message>-->
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="dtb:list/dtb:pagenum" mode="pagenumInLi">
		<xsl:call-template name="pagenum"/>
		<xsl:apply-templates select="following-sibling::*[1][self::dtb:pagenum]" mode="pagenumInLi"
		/>
	</xsl:template>



	<xsl:template match="dtb:list/dtb:prodnote">
		<li class="optional-prodnote">
			<xsl:apply-templates/>
			<xsl:apply-templates select="following-sibling::*[1][self::dtb:pagenum]"
				mode="pagenumInLi"/>
		</li>
	</xsl:template>

	<xsl:template match="dtb:blockquote/dtb:pagenum">
		<div class="dummy">
			<xsl:call-template name="pagenum"/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:address">
		<div class="address">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:h1 | dtb:h2 | dtb:h3 | dtb:h4 | dtb:h5 | dtb:h6">
		<xsl:element name="{local-name()}">
			<xsl:call-template name="copyCatts"/>
			<xsl:if test="not(@id)">
				<xsl:attribute name="id">
					<xsl:value-of select="generate-id()"/>
				</xsl:attribute>
			</xsl:if>
			<xsl:apply-templates/>
		</xsl:element>
	</xsl:template>

	<xsl:template match="dtb:bridgehead">
		<div class="bridgehead">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>


	<xsl:template match="dtb:list[not(@type)]">
		<ul>
			<xsl:copy-of select="@start"/>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</ul>
	</xsl:template>


	<xsl:template match="dtb:lic">
		<span class="lic">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>

	<xsl:template match="dtb:br">
		<xsl:element name="br">
			<xsl:copy-of select="@id | @class | @title"/>
		</xsl:element>
	</xsl:template>


	<xsl:template match="dtb:noteref">
		<a class="noteref">
			<xsl:call-template name="copyCncatts"/>
			<xsl:attribute name="href">
				<xsl:if test="not(contains(@idref, '#'))">
					<xsl:text>#</xsl:text>
				</xsl:if>
				<xsl:value-of select="@idref"/>
			</xsl:attribute>
			<xsl:apply-templates/>
		</a>
	</xsl:template>


	<xsl:template match="dtb:img">
		<img>
			<xsl:call-template name="copyCatts"/>
			<xsl:copy-of select="@src | @alt | @longdesc | @height | @width"/>
		</img>
	</xsl:template>


	<xsl:template match="dtb:caption">
		<caption>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates mode="inlineOnly"/>
		</caption>
	</xsl:template>


	<xsl:template match="dtb:imggroup/dtb:caption">
		<div class="caption">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:div">
		<div>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:imggroup">
		<xsl:call-template name="inlineParent">
			<xsl:with-param name="class" select="'imggroup'"/>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="dtb:prodnote">
		<xsl:call-template name="inlineParent">
			<xsl:with-param name="class" select="'optional-prodnote'"/>
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="dtb:annotation">
		<div class="annotation">
			<xsl:call-template name="copyCncatts"/>
			<xsl:variable name="idref" select="concat('#', @id)"/>
			<xsl:variable name="refs" select="//dtb:annoref[@id and @idref=$idref]"/>
			<xsl:if test="$refs">
				<p>
					<xsl:for-each select="$refs">
						<a href="{concat('#', @id)}">
							<xsl:apply-templates/>
						</a>
					</xsl:for-each>
				</p>				
			</xsl:if>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:author">
		<div class="author">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:blockquote">
		<blockquote>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</blockquote>
	</xsl:template>


	<xsl:template match="dtb:byline">
		<div class="byline">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:dateline">
		<div class="dateline">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:doctitle[1]">
		<h1 class="title">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</h1>
	</xsl:template>

	<xsl:template match="dtb:doctitle[position()&gt;1]">
		<p class="doctitle">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</p>
	</xsl:template>

	<xsl:template match="dtb:docauthor">
		<p class="docauthor">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</p>
	</xsl:template>

	<xsl:template match="dtb:epigraph">
		<div class="epigraph">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:note">
		<div class="notebody">
			<xsl:call-template name="copyCncatts"/>
			<xsl:variable name="idref" select="concat('#', @id)"/>
			<xsl:variable name="refs" select="//dtb:noteref[@id and @idref=$idref]"/>
			<xsl:if test="$refs">
				<p>
					<xsl:for-each select="$refs">
						<a href="{concat('#', @id)}">
							<xsl:apply-templates/>
						</a>
					</xsl:for-each>
				</p>				
			</xsl:if>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:sidebar">
		<div class="sidebar">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>

	<xsl:template match="dtb:hd">
		<xsl:choose>
			<xsl:when test="parent::dtb:level">
				<xsl:variable name="depth">
					<xsl:value-of select="count(ancestor-or-self::dtb:level/dtb:hd)"/>
				</xsl:variable>

				<xsl:choose>
					<xsl:when test="$depth &lt; 7">
						<xsl:element name="{concat('h', $depth)}">
							<xsl:call-template name="copyCatts"/>
							<xsl:if test="not(@id)">
								<xsl:attribute name="id">
									<xsl:value-of select="generate-id()"/>
								</xsl:attribute>
							</xsl:if>
							<xsl:apply-templates/>
						</xsl:element>
					</xsl:when>
					<xsl:otherwise>
						<div>
							<xsl:attribute name="class">
								<xsl:value-of select="concat('h', $depth)"/>
							</xsl:attribute>
							<xsl:call-template name="copyCncatts"/>
							<xsl:apply-templates/>
							<xsl:if test="not(@id)">
								<xsl:attribute name="id">
									<xsl:value-of select="generate-id()"/>
								</xsl:attribute>
							</xsl:if>
						</div>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:when>
			<xsl:otherwise>
				<div class="hd">
					<xsl:call-template name="copyCncatts"/>
					<xsl:apply-templates/>
				</div>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template match="dtb:list/dtb:hd">
		<li class="hd">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
			<xsl:apply-templates select="following-sibling::*[1][self::dtb:pagenum]"
				mode="pagenumInLi"/>
		</li>
	</xsl:template>




	<xsl:template match="dtb:list[@type = 'ol']">
		<ol>
			<xsl:copy-of select="@start"/>
			<xsl:choose>
				<xsl:when test="@enum = 'i'">
					<xsl:attribute name="class">lower-roman</xsl:attribute>
				</xsl:when>
				<xsl:when test="@enum = 'I'">
					<xsl:attribute name="class">upper-roman</xsl:attribute>
				</xsl:when>
				<xsl:when test="@enum = 'a'">
					<xsl:attribute name="class">lower-alpha</xsl:attribute>
				</xsl:when>
				<xsl:when test="@enum = 'A'">
					<xsl:attribute name="class">upper-alpha</xsl:attribute>
				</xsl:when>
			</xsl:choose>
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</ol>
	</xsl:template>





	<xsl:template match="dtb:list[@type = 'ul']">
		<ul>
			<xsl:copy-of select="@start"/>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</ul>
	</xsl:template>

	<xsl:template match="dtb:list[@type = 'pl']">
		<ul class="plain">
			<xsl:copy-of select="@start"/>
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</ul>
	</xsl:template>

	<xsl:template match="dtb:li">
		<li>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
			<xsl:apply-templates select="following-sibling::*[1][self::dtb:pagenum]"
				mode="pagenumInLi"/>
		</li>
	</xsl:template>



	<xsl:template match="dtb:dl">
		<dl>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</dl>
	</xsl:template>

	<xsl:template match="dtb:dl/dtb:pagenum" priority="1">
		<dt>
			<xsl:call-template name="pagenum"/>
		</dt>
		<dd>
			<xsl:comment>empty</xsl:comment>
		</dd>
	</xsl:template>

	<xsl:template match="dtb:dt">
		<dt>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</dt>
	</xsl:template>

	<xsl:template match="dtb:dd">
		<dd>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</dd>
	</xsl:template>


	<xsl:template match="dtb:table">
		<table>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</table>
	</xsl:template>

	<xsl:template match="dtb:blockquote/dtb:table/dtb:pagenum" mode="pagenumonly">
		<div class="dummy">
			<xsl:call-template name="pagenum"/>
		</div>
	</xsl:template>


	<xsl:template match="dtb:pagenum" mode="pagenumonly">
		<xsl:call-template name="pagenum"/>
	</xsl:template>

	<xsl:template match="dtb:table/dtb:pagenum | dtb:tbody/dtb:pagenum">
		<tr>
			<td class="noborder">
				<xsl:attribute name="colspan">
					<xsl:variable name="tdsInRow"
						select="number(sum(ancestor::dtb:table[1]/descendant::*[self::dtb:td or self::dtb:th]/(@colspan * @rowspan))) div count(ancestor::dtb:table[1]/descendant::dtb:tr)"/>
					<!-- <xsl:message>tdsInRow:<xsl:value-of select="$tdsInRow"/></xsl:message> -->
					<xsl:if test="$tdsInRow != round($tdsInRow) and $tdsInRow != NaN">
						<xsl:message>Warning: Colspan and rowspan values in table don't add
							up.</xsl:message>
					</xsl:if>
					<xsl:value-of select="floor($tdsInRow)"/>
				</xsl:attribute>
				<xsl:call-template name="pagenum"/>
			</td>
		</tr>
	</xsl:template>

	<xsl:template match="dtb:tbody">
		<tbody>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</tbody>
	</xsl:template>



	<xsl:template match="dtb:thead">
		<thead>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</thead>
	</xsl:template>

	<xsl:template match="dtb:tfoot">
		<tfoot>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</tfoot>
	</xsl:template>

	<xsl:template match="dtb:tr">
		<tr>
			<xsl:call-template name="copyCatts"/>
			<xsl:copy-of select="@rowspan | @colspan"/>
			<xsl:apply-templates/>
		</tr>
	</xsl:template>

	<xsl:template match="dtb:th">
		<th>
			<xsl:call-template name="copyCatts"/>
			<xsl:copy-of select="@rowspan | @colspan"/>
			<xsl:apply-templates/>
		</th>
	</xsl:template>

	<xsl:template match="dtb:td">
		<td>
			<xsl:call-template name="copyCatts"/>
			<xsl:copy-of select="@rowspan | @colspan"/>
			<xsl:apply-templates/>
		</td>
	</xsl:template>

	<xsl:template match="dtb:colgroup">
		<colgroup>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</colgroup>
	</xsl:template>

	<xsl:template match="dtb:col">
		<col>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</col>
	</xsl:template>








	<xsl:template match="dtb:poem">
		<div class="poem">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>


	<xsl:template match="dtb:poem/dtb:title">
		<p class="title">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</p>
	</xsl:template>

	<xsl:template match="dtb:cite/dtb:title">
		<span class="title">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>

	<xsl:template match="dtb:cite/dtb:author">
		<span class="author">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>

	<xsl:template match="dtb:cite">
		<cite>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</cite>
	</xsl:template>



	<xsl:template match="dtb:code">
		<code>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</code>
	</xsl:template>

	<xsl:template match="dtb:kbd">
		<kbd>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</kbd>
	</xsl:template>

	<xsl:template match="dtb:q">
		<q>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
			<!--<xsl:apply-templates/>-->
		</q>
	</xsl:template>

	<xsl:template match="dtb:samp">
		<samp>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</samp>
	</xsl:template>



	<xsl:template match="dtb:linegroup">
		<div class="linegroup">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</div>
	</xsl:template>


	<xsl:template match="dtb:line">
		<p class="line">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates mode="inlineOnly"/>
		</p>
	</xsl:template>

	<xsl:template match="dtb:linenum">
		<span class="linenum">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>







	<!-- Inlines -->

	<xsl:template match="dtb:a">
		<span class="anchor">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>

	<xsl:template match="dtb:em">
		<em>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</em>
	</xsl:template>

	<xsl:template match="dtb:strong">
		<strong>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</strong>
	</xsl:template>


	<xsl:template match="dtb:abbr">
		<abbr>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</abbr>
	</xsl:template>

	<xsl:template match="dtb:acronym">
		<acronym>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</acronym>
	</xsl:template>

	<xsl:template match="dtb:bdo">
		<bdo>
			<xsl:call-template name="copyCatts"/>
			<xsl:copy-of select="@dir"/>
			<xsl:apply-templates/>
		</bdo>
	</xsl:template>

	<xsl:template match="dtb:dfn">
		<span class="definition">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>

	<xsl:template match="dtb:sent">
		<span class="sentence">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>


	<xsl:template match="dtb:w">
		<span class="word">
			<xsl:call-template name="copyCncatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>




	<xsl:template match="dtb:sup">
		<sup>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</sup>
	</xsl:template>

	<xsl:template match="dtb:sub">
		<sub>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</sub>
	</xsl:template>


	<xsl:template match="dtb:span">
		<span>
			<xsl:call-template name="copyCatts"/>
			<xsl:apply-templates/>
		</span>
	</xsl:template>


	<!-- FIXME internal and external -->
	<xsl:template match="dtb:a[@href]">
		<a>
			<xsl:call-template name="copyCatts"/>
			<xsl:copy-of select="@href"/>
		</a>
	</xsl:template>

	<xsl:template match="dtb:annoref">
		<a class="annoref">
			<xsl:call-template name="copyCncatts"/>
			<xsl:attribute name="href">
				<xsl:text>#</xsl:text>
				<xsl:value-of select="translate(@idref, '#', '')"/>
			</xsl:attribute>
			<xsl:apply-templates/>
		</a>
	</xsl:template>

	<xsl:template match="dtb:*">
		<xsl:message> *****<xsl:value-of select="name(..)"/>/{<xsl:value-of select="namespace-uri()"
				/>}<xsl:value-of select="name()"/>****** </xsl:message>
	</xsl:template>

	<!--   <!ENTITY isInline "self::dtb:a or self::dtb:em or self::dtb:strong or self::dtb:abbr or self::dtb:acronym or self::dtb:bdo or self::dtb:dfn or self::dtb:sent or self::dtb:w or self::dtb:sup or self::dtb:sub or self::dtb:span or self::dtb:annoref or self::dtb:noteref or self::dtb:img or self::dtb:br or self::dtb:q or self::dtb:samp or self::dtb:pagenum"> -->
	<xsl:template match="dtb:*" mode="inlineOnly">
		<xsl:choose>
			<xsl:when
				test="self::dtb:a or self::dtb:em or self::dtb:strong or self::dtb:abbr or self::dtb:acronym or self::dtb:bdo or self::dtb:dfn or self::dtb:sent or self::dtb:w or self::dtb:sup or self::dtb:sub or self::dtb:span or self::dtb:annoref or self::dtb:noteref or self::dtb:img or self::dtb:br or self::dtb:q or self::dtb:samp or self::dtb:pagenum">
				<xsl:apply-templates select=".">
					<xsl:with-param name="inlineFix" select="'true'"/>
				</xsl:apply-templates>
			</xsl:when>
			<xsl:otherwise>
				<span>
					<xsl:call-template name="get_class_attribute">
						<xsl:with-param name="element" select="."/>
					</xsl:call-template>
					<xsl:call-template name="copyCncatts"/>
					<xsl:apply-templates mode="inlineOnly"/>
				</span>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	<xsl:template name="get_class_attribute">
		<xsl:param name="element"/>
		<xsl:choose>
			<xsl:when test="name($element) = 'imggroup'">
				<xsl:attribute name="class">imggroup</xsl:attribute>
			</xsl:when>
			<xsl:when test="name($element) = 'caption'">
				<xsl:attribute name="class">caption</xsl:attribute>
			</xsl:when>
			<xsl:when test="$element/@class">
				<xsl:attribute name="class">
					<xsl:value-of select="$element/@class"/>
				</xsl:attribute>
			</xsl:when>
			<xsl:otherwise>
				<xsl:attribute name="class">
					<xsl:text>inline-</xsl:text>
					<xsl:value-of select="name($element)"/>
				</xsl:attribute>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>


	<!-- MathML -->
	<xsl:template match="m:math">
		<xsl:copy-of select="."/>
	</xsl:template>

	<xsl:template match="m:math" mode="inlineOnly">
		<xsl:copy-of select="."/>
	</xsl:template>

	<!-- SVG; deep copy -->
	<xsl:template match="svg:svg">
		<xsl:copy-of select="."/>
	</xsl:template>

	<xsl:template match="svg:svg" mode="inlineOnly">
		<xsl:copy-of select="."/>
	</xsl:template>

	<xsl:template name="getdepth">
		<xsl:param name="node" select="."/>
		<xsl:value-of
			select="count($node/ancestor-or-self::dtb:level) + count($node/ancestor-or-self::dtb:level1) + count($node/ancestor-or-self::dtb:level2) + count($node/ancestor-or-self::dtb:level3) + count($node/ancestor-or-self::dtb:level4) + count($node/ancestor-or-self::dtb:level5) + count($node/ancestor-or-self::dtb:level6)"
		/>
	</xsl:template>

	<xsl:template name="whereisnoteref">
		<xsl:param name="idref"/>
		<xsl:variable name="id">
			<xsl:value-of select="//*[@idref = concat('#', $idref)]/@id"/>
		</xsl:variable>
		<xsl:value-of select="$id"/>
	</xsl:template>

</xsl:stylesheet>
